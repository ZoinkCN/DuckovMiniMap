using MiniMap.Utils;
using UnityEngine;
using MiniMap.Managers;

namespace MiniMap.Poi
{
    /// <summary>
    /// 方向指示器兴趣点
    /// 负责显示角色朝向箭头，角度更新由DistanceBasedUpdateManager异步处理
    /// </summary>
    public class DirectionPointOfInterest : CharacterPointOfInterestBase
    {
        private float rotationEulerAngle;
        private float baseEulerAngle;

        /// <summary>
        /// 旋转角度（0-360度）
        /// </summary>
        public float RotationEulerAngle { 
            get => rotationEulerAngle % 360; 
            set => rotationEulerAngle = value % 360; 
        }
        
        /// <summary>
        /// 基础旋转角度
        /// </summary>
        public float BaseEulerAngle { 
            get => baseEulerAngle % 360; 
            set => baseEulerAngle = value % 360; 
        }
        
        /// <summary>
        /// 实际旋转角度 = 基础角度 + 旋转角度
        /// </summary>
        public float RealEulerAngle => (baseEulerAngle + rotationEulerAngle) % 360;
        
        /// <summary>
        /// 显示名称（方向指示器不需要名称）
        /// </summary>
        public override string DisplayName => string.Empty;
        
        /// <summary>
        /// 是否为区域兴趣点
        /// </summary>
        public override bool IsArea => false;
        
        /// <summary>
        /// 区域半径
        /// </summary>
        public override float AreaRadius => 0;
        
        /// <summary>
        /// 颜色
        /// </summary>
        public override Color Color => Color.white;

        /// <summary>
        /// 启用时注册到距离分层管理器
        /// 注意：移除了Update方法，角度更新由DistanceBasedUpdateManager异步处理
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            
            // 注册到距离分层管理器，由管理器根据距离决定是否更新角度
            DistanceBasedUpdateManager.Instance?.RegisterPoi(this);
        }

        /// <summary>
        /// 禁用时从距离分层管理器注销
        /// </summary>
        protected override void OnDisable()
        {
            // 从距离分层管理器注销
            DistanceBasedUpdateManager.Instance?.UnregisterPoi(this);
            
            base.OnDisable();
        }
        
        // 注意：移除了Update方法，因为：
        // 1. 死亡检查将用事件系统处理（后续实现）
        // 2. 角度更新由DistanceBasedUpdateManager异步处理
        // 3. 15米外的POI不更新角度，15米内的POI按5Hz频率更新
    }
}