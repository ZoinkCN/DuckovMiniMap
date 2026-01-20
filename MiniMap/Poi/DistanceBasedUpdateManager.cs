using Cysharp.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using MiniMap.Poi;
using System;

namespace MiniMap.Managers
{
    /// <summary>
    /// 基于距离的分层更新管理器
    /// 15米内的POI进行角度更新，15米外的POI只初始化一次不更新
    /// 使用UniTask异步循环替代Update，大幅提升性能
    /// </summary>
    public class DistanceBasedUpdateManager : MonoBehaviour
    {
        private static DistanceBasedUpdateManager instance;
        public static DistanceBasedUpdateManager Instance => instance;
        
        private CancellationTokenSource globalCts;
        
        // 使用并发集合保证线程安全
        private readonly ConcurrentDictionary<DirectionPointOfInterest, PoiData> nearbyPois = new();  // 15米内，需要更新角度
        private readonly ConcurrentDictionary<DirectionPointOfInterest, PoiData> distantPois = new(); // 15米外，不更新角度
        
        // 配置参数
        private const float UpdateIntervalMs = 200f;           // 0.2秒更新一次 = 5Hz
        private const float DistanceThreshold = 15f;           // 15米距离阈值
        private const float DistanceThresholdSqr = 225f;       // 15² = 225，用于避免开方运算
        private const float AngleChangeThreshold = 3f;         // 角度变化阈值，超过3度才重新计算
        private const int NearbyBatchSize = 10;                // 近处每批处理10个POI
        private const int DistantBatchSize = 20;               // 远处每批处理20个（只检查距离）
        
        // 性能监控
        private int nearbyUpdates;
        private int distantChecks;
        private int layerChanges;
        private float lastLogTime;
        
        /// <summary>
        /// POI数据封装类
        /// </summary>
        private class PoiData
        {
            public Vector3 LastPosition;
            public Vector3 LastAimDirection;
            public float LastCalculatedAngle;
            public float LastUpdateTime;
            public bool IsInitialized;
        }
        
        void Awake()
        {
            // 单例模式
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        void OnEnable()
        {
            globalCts = new CancellationTokenSource();
            
            // 启动两个异步循环
            StartMainUpdateLoop().Forget();
            StartLayerCheckLoop().Forget();
            
            // 初始化性能监控计数器
            nearbyUpdates = 0;
            distantChecks = 0;
            layerChanges = 0;
            lastLogTime = Time.time;
        }
        
        void OnDisable()
        {
            // 取消所有异步任务
            globalCts?.Cancel();
            globalCts?.Dispose();
            globalCts = null;
            
            // 记录最终性能统计
            LogPerformanceStats();
        }
        
        /// <summary>
        /// 注册方向指示器POI，根据距离自动分层
        /// </summary>
        public void RegisterPoi(DirectionPointOfInterest directionPoi)
        {
            if (directionPoi == null || directionPoi.Character == null)
            {
                return;
            }
            
            Vector3 playerPos = GetPlayerPosition();
            Vector3 poiPos = directionPoi.Character.transform.position;
            float distanceSqr = (playerPos - poiPos).sqrMagnitude;
            
            PoiData data = new PoiData
            {
                LastPosition = poiPos,
                LastAimDirection = GetAimDirection(directionPoi),
                LastUpdateTime = Time.time,
                IsInitialized = false
            };
            
            // 根据距离决定初始分层
            if (distanceSqr <= DistanceThresholdSqr)
            {
                // 15米内：正常初始化并更新角度
                data.LastCalculatedAngle = Vector3.SignedAngle(Vector3.forward, data.LastAimDirection, Vector3.up);
                directionPoi.RotationEulerAngle = data.LastCalculatedAngle;
                data.IsInitialized = true;
                _ = nearbyPois.TryAdd(directionPoi, data);
            }
            else
            {
                // 15米外：只初始化一次，不计算角度，使用默认角度
                directionPoi.RotationEulerAngle = 0f;
                data.IsInitialized = true;
                _ = distantPois.TryAdd(directionPoi, data);
            }
        }
        
        /// <summary>
        /// 注销方向指示器POI
        /// </summary>
        public void UnregisterPoi(DirectionPointOfInterest directionPoi)
        {
            if (directionPoi != null)
            {
                _ = nearbyPois.TryRemove(directionPoi, out _);
                _ = distantPois.TryRemove(directionPoi, out _);
            }
        }
        
        /// <summary>
        /// 主更新循环：处理近处POI的角度计算
        /// </summary>
        private async UniTaskVoid StartMainUpdateLoop()
        {
            CancellationToken ct = globalCts.Token;
            
            try
            {
                // 初始随机延迟，错开启动时间
                await UniTask.Delay(UnityEngine.Random.Range(0, 100), cancellationToken: ct);
                
                while (!ct.IsCancellationRequested)
                {
                    float startTime = Time.realtimeSinceStartup;
                    
                    // 更新近处的POI（需要角度计算）
                    await UpdateNearbyPoisAsync(ct);
                    
                    // 动态调整等待时间，确保总间隔约为UpdateIntervalMs
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    int waitTime = Mathf.Max(1, (int)(UpdateIntervalMs - elapsed * 1000));
                    await UniTask.Delay(waitTime, cancellationToken: ct);
                    
                    // 每10秒记录一次性能日志
                    if (Time.time - lastLogTime > 10f)
                    {
                        LogPerformanceStats();
                        lastLogTime = Time.time;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不记录错误
            }
        }
        
        /// <summary>
        /// 分层检查循环：检查POI是否需要远近层移动（1Hz频率）
        /// </summary>
        private async UniTaskVoid StartLayerCheckLoop()
        {
            CancellationToken ct = globalCts.Token;
            
            try
            {
                // 以1Hz频率检查分层变化，低频检查避免性能开销
                while (!ct.IsCancellationRequested)
                {
                    await UniTask.Delay(1000, cancellationToken: ct);
                    CheckAndUpdateLayers();
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
        }
        
        /// <summary>
        /// 检查并更新POI的分层（近处↔远处）
        /// </summary>
        private void CheckAndUpdateLayers()
        {
            Vector3 playerPos = GetPlayerPosition();
            System.Collections.Generic.List<DirectionPointOfInterest> distantToMove = new System.Collections.Generic.List<DirectionPointOfInterest>();
            System.Collections.Generic.List<DirectionPointOfInterest> nearbyToMove = new System.Collections.Generic.List<DirectionPointOfInterest>();
            
            // 检查远处的POI是否进入了近处
            foreach (System.Collections.Generic.KeyValuePair<DirectionPointOfInterest, PoiData> kvp in distantPois)
            {
                DirectionPointOfInterest poi = kvp.Key;
                PoiData data = kvp.Value;
                
                if (poi == null || poi.Character == null)
                {
                    continue;
                }
                
                float distanceSqr = (playerPos - poi.Character.transform.position).sqrMagnitude;
                if (distanceSqr <= DistanceThresholdSqr)
                {
                    // 从远处移动到近处
                    distantToMove.Add(poi);
                }
            }
            
            // 检查近处的POI是否移动到了远处
            foreach (System.Collections.Generic.KeyValuePair<DirectionPointOfInterest, PoiData> kvp in nearbyPois)
            {
                DirectionPointOfInterest poi = kvp.Key;
                PoiData data = kvp.Value;
                
                if (poi == null || poi.Character == null)
                {
                    continue;
                }
                
                float distanceSqr = (playerPos - poi.Character.transform.position).sqrMagnitude;
                if (distanceSqr > DistanceThresholdSqr)
                {
                    // 从近处移动到远处
                    nearbyToMove.Add(poi);
                }
            }
            
            // 执行分层移动
            foreach (DirectionPointOfInterest poi in distantToMove)
            {
                if (distantPois.TryRemove(poi, out PoiData data))
                {
                    // 进入近处范围，需要初始化角度并开始更新
                    data.LastAimDirection = GetAimDirection(poi);
                    data.LastCalculatedAngle = Vector3.SignedAngle(Vector3.forward, data.LastAimDirection, Vector3.up);
                    poi.RotationEulerAngle = data.LastCalculatedAngle;
                    _ = nearbyPois.TryAdd(poi, data);
                    layerChanges++;
                }
            }
            
            foreach (DirectionPointOfInterest poi in nearbyToMove)
            {
                if (nearbyPois.TryRemove(poi, out PoiData data))
                {
                    // 移动到远处，停止角度更新
                    _ = distantPois.TryAdd(poi, data);
                    layerChanges++;
                }
            }
        }
        
        /// <summary>
        /// 更新近处的POI（需要角度计算）
        /// </summary>
        private async UniTask UpdateNearbyPoisAsync(CancellationToken ct)
        {
            if (nearbyPois.IsEmpty)
            {
                return;
            }
            
            int processed = 0;
            int batchSize = Mathf.Min(NearbyBatchSize, nearbyPois.Count);
            
            foreach (System.Collections.Generic.KeyValuePair<DirectionPointOfInterest, PoiData> kvp in nearbyPois)
            {
                if (processed >= batchSize)
                {
                    break;
                }
                
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                
                DirectionPointOfInterest poi = kvp.Key;
                PoiData data = kvp.Value;
                
                // 有效性检查
                if (poi == null || !poi.gameObject.activeInHierarchy || poi.Character == null)
                {
                    _ = nearbyPois.TryRemove(poi, out _);
                    continue;
                }
                
                // 双重检查：确保POI还在近处范围
                Vector3 playerPos = GetPlayerPosition();
                float distanceSqr = (playerPos - poi.Character.transform.position).sqrMagnitude;
                if (distanceSqr > DistanceThresholdSqr)
                {
                    // 意外移动到远处，移到相应集合
                    if (nearbyPois.TryRemove(poi, out data))
                    {
                        _ = distantPois.TryAdd(poi, data);
                        layerChanges++;
                    }
                    continue;
                }
                
                // 检查方向变化是否超过阈值
                Vector3 currentAimDirection = GetAimDirection(poi);
                float angleChange = Vector3.Angle(currentAimDirection, data.LastAimDirection);
                
                if (angleChange > AngleChangeThreshold)
                {
                    // 需要更新角度
                    await UpdatePoiAngleAsync(poi, data, currentAimDirection, ct);
                    nearbyUpdates++;
                    processed++;
                }
                
                distantChecks++; // 统计检查次数
            }
        }
        
        /// <summary>
        /// 更新远处的POI（只检查距离，不更新角度）
        /// 此方法在分层检查循环中调用
        /// </summary>
        private void UpdateDistantPois()
        {
            if (distantPois.IsEmpty)
            {
                return;
            }
            
            Vector3 playerPos = GetPlayerPosition();
            int processed = 0;
            int batchSize = Mathf.Min(DistantBatchSize, distantPois.Count);
            
            foreach (System.Collections.Generic.KeyValuePair<DirectionPointOfInterest, PoiData> kvp in distantPois)
            {
                if (processed >= batchSize)
                {
                    break;
                }
                
                DirectionPointOfInterest poi = kvp.Key;
                PoiData data = kvp.Value;
                
                if (poi == null || poi.Character == null)
                {
                    continue;
                }
                
                // 只检查距离，不更新角度
                float distanceSqr = (playerPos - poi.Character.transform.position).sqrMagnitude;
                data.LastPosition = poi.Character.transform.position;
                
                distantChecks++;
                processed++;
            }
        }
        
        /// <summary>
        /// 更新单个POI的角度
        /// </summary>
        private async UniTask UpdatePoiAngleAsync(
            DirectionPointOfInterest poi, 
            PoiData data, 
            Vector3 currentAimDirection,
            CancellationToken ct)
        {
            try
            {
                // Unity对象操作必须在主线程
                await UniTask.SwitchToMainThread(cancellationToken: ct);
                
                float newAngle = Vector3.SignedAngle(Vector3.forward, currentAimDirection, Vector3.up);
                
                data.LastAimDirection = currentAimDirection;
                data.LastCalculatedAngle = newAngle;
                data.LastUpdateTime = Time.time;
                
                poi.RotationEulerAngle = newAngle;
            }
            catch (System.Exception ex)
            {
                ModBehaviour.Logger.LogWarning($"更新POI角度失败: {ex.Message}");
                _ = nearbyPois.TryRemove(poi, out _);
            }
        }
        
        /// <summary>
        /// 获取玩家当前位置
        /// </summary>
        private Vector3 GetPlayerPosition()
        {
            if (LevelManager.Instance?.MainCharacter == null)
            {
                return Vector3.zero;
            }
            
            return LevelManager.Instance.MainCharacter.transform.position;
        }
        
        /// <summary>
        /// 获取角色的瞄准方向
        /// </summary>
        private Vector3 GetAimDirection(DirectionPointOfInterest poi)
        {
            if (poi.Character == null)
            {
                return Vector3.forward;
            }
            
            if (poi.Character.IsMainCharacter)
            {
                // 玩家角色：根据配置获取朝向
                string facingBase = ModSettingManager.GetActualDropdownValue("facingBase", false);
                return facingBase == "Mouse"
                    ? LevelManager.Instance.InputManager.InputAimPoint - poi.Character.transform.position
                    : poi.Character.movementControl.targetAimDirection;
            }
            else
            {
                // NPC角色：使用目标瞄准方向
                return poi.Character.movementControl.targetAimDirection;
            }
        }
        
        /// <summary>
        /// 记录性能统计信息
        /// </summary>
        private void LogPerformanceStats()
        {
            int totalPois = nearbyPois.Count + distantPois.Count;
            if (totalPois == 0)
            {
                return;
            }
            
            float nearbyPercent = (float)nearbyPois.Count / totalPois * 100f;
            float updateRate = nearbyPois.Count > 0 ? (float)nearbyUpdates / nearbyPois.Count : 0;
            
            ModBehaviour.Logger.Log(
                $"[距离分层] 总数: {totalPois} (近: {nearbyPois.Count}, 远: {distantPois.Count}, {nearbyPercent:F1}%在近处)\n" +
                $"更新: {nearbyUpdates}, 检查: {distantChecks}, 分层变动: {layerChanges}\n" +
                $"近处更新率: {updateRate:F2}次/POI, 分层检查: 1Hz"
            );
            
            // 重置计数器
            nearbyUpdates = 0;
            distantChecks = 0;
            layerChanges = 0;
        }
        
        /// <summary>
        /// 强制更新所有POI（配置变更时调用）
        /// </summary>
        public void ForceUpdateAll()
        {
            Vector3 playerPos = GetPlayerPosition();
            
            // 强制重新分层并更新近处POI
            foreach (System.Collections.Generic.KeyValuePair<DirectionPointOfInterest, PoiData> kvp in nearbyPois)
            {
                DirectionPointOfInterest poi = kvp.Key;
                PoiData data = kvp.Value;
                
                if (poi != null)
                {
                    data.LastAimDirection = Vector3.zero; // 强制下次更新
                    poi.RotationEulerAngle = 0f;
                }
            }
        }
        
        /// <summary>
        /// 获取统计信息（用于调试）
        /// </summary>
        public (int nearby, int distant, int total) GetStats()
        {
            return (nearbyPois.Count, distantPois.Count, nearbyPois.Count + distantPois.Count);
        }
    }
}