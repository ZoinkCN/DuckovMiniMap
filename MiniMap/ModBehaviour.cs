using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Duckov.Modding;
using MiniMap.Patchers;
using MiniMap.Managers;
using MiniMap.Utils;
using ZoinkModdingLibrary.Patcher;
using ZoinkModdingLibrary;
using MiniMap.Compatibility.BetterMapMarker.Patchers;
using Unity.VisualScripting;
using MiniMap.Compatibility;

namespace MiniMap
{

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static readonly string MOD_ID = "com.zoink.minimap";

        public static readonly string MOD_NAME = "MiniMap";
        public static ModLogger Logger { get; } = new ModLogger(MOD_NAME);
        public static Harmony Harmony { get; } = new Harmony(MOD_ID);

        public static ModBehaviour? Instance { get; private set; }

        private List<PatcherBase> patchers = new List<PatcherBase>() {
            CharacterSpawnerRootPatcher.Instance,
            PointOfInterestEntryPatcher.Instance,
            MiniMapCompassPatcher.Instance,
            MiniMapDisplayPatcher.Instance,
            MapMarkerManagerPatcher.Instance,
        };
		
		private DistanceBasedUpdateManager? distanceUpdateManager;

        public bool PatchSingleExtender(Type targetType, Type extenderType, string methodName, BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            MethodInfo originMethod = targetType.GetMethod(methodName, bindFlags);
            if (originMethod == null)
            {
                Debug.LogWarning($"[{MOD_NAME}] Original method not found: {targetType.Name}.{methodName}");
                return false;
            }

            try
            {
                MethodInfo prefix = extenderType.GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                MethodInfo postfix = extenderType.GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                MethodInfo transpiler = extenderType.GetMethod("Transpiler", BindingFlags.Static | BindingFlags.Public);
                MethodInfo finalizer = extenderType.GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public);
                Harmony.Unpatch(originMethod, HarmonyPatchType.All, Harmony.Id);
                Harmony.Patch(
                    originMethod,
                    prefix != null ? new HarmonyMethod(prefix) : null,
                    postfix != null ? new HarmonyMethod(postfix) : null,
                    transpiler != null ? new HarmonyMethod(transpiler) : null,
                    finalizer != null ? new HarmonyMethod(finalizer) : null
                );
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{MOD_NAME}] Failed to patch {originMethod}: {ex.Message}");
                return false;
            }
        }

        public bool UnpatchSingleExtender(string assembliyName, string targetTypeName, string methodName, BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            Type? targetType = AssemblyOption.FindTypeInAssemblies(assembliyName, targetTypeName);
            if (targetType == null)
            {
                Debug.LogWarning($"[{MOD_NAME}] Target Type \"{targetTypeName}\" Not Found!");
                return false;
            }
            MethodInfo originMethod = targetType.GetMethod(methodName, bindFlags);
            Harmony.Unpatch(originMethod, HarmonyPatchType.All, MOD_ID);
            return true;
        }

        void ApplyHarmonyPatchers()
        {
            try
            {
                Logger.Log($"Patching Patchers");
                foreach (var patcher in patchers)
                {
                    patcher.Setup(Harmony, Logger).Patch();
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"应用扩展器失败: {e}");
            }
        }
        void CancelHarmonyPatchers()
        {
            try
            {
                foreach (var patcher in patchers)
                {
                    patcher.Unpatch();
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"取消扩展器失败: {e}");
            }
        }
		
		/// <summary>
		/// 初始化距离分层更新管理器
		/// 该管理器负责：
		/// 1. 15米内的POI进行角度更新（5Hz频率）
		/// 2. 15米外的POI只初始化一次，不更新角度
		/// 3. 使用UniTask异步循环替代Update，大幅提升性能
		/// </summary>
		private void InitializeDistanceUpdateManager()
		{
			GameObject managerObject = new GameObject("DistanceBasedUpdateManager");
			managerObject.transform.SetParent(transform);
			distanceUpdateManager = managerObject.AddComponent<DistanceBasedUpdateManager>();
			
			Logger.Log($"已初始化距离分层更新管理器");
		}
		
        void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"ModBehaviour 已实例化");
                return;
            }
            Instance = this;
            gameObject.GetOrAddComponent<CompatibilityManager>();
        }

        void OnEnable()
        {
            try
            {
				InitializeDistanceUpdateManager();
                CustomMinimapManager.Initialize();
                ApplyHarmonyPatchers();
                ModManager.OnModActivated += ModManager_OnModActivated;
                LevelManager.OnEvacuated += OnEvacuated;
                //SceneLoader.onFinishedLoadingScene += PoiManager.OnFinishedLoadingScene;
                //LevelManager.OnAfterLevelInitialized += PoiManager.OnLenvelIntialized;
				LevelManager.OnAfterLevelInitialized += ModSettingManager.CreateUI;

            }
            catch (Exception e)
            {
                Logger.LogError($"启用mod失败: {e}");
            }
        }

        void OnEvacuated(EvacuationInfo _info)
        {
            CustomMinimapManager.Hide();
        }

        void OnDisable()
        {
            try
            {
                CancelHarmonyPatchers();
                ModManager.OnModActivated -= ModManager_OnModActivated;
                LevelManager.OnEvacuated -= OnEvacuated;
                //SceneLoader.onFinishedLoadingScene -= PoiManager.OnFinishedLoadingScene;
                //LevelManager.OnAfterLevelInitialized -= PoiManager.OnLenvelIntialized;
				LevelManager.OnAfterLevelInitialized -= ModSettingManager.CreateUI;
				
				if (distanceUpdateManager != null)
				{
					GameObject.Destroy(distanceUpdateManager.gameObject);
					distanceUpdateManager = null;
					Logger.Log($"已销毁距离分层更新管理器");
				}
				
                CustomMinimapManager.Destroy();
                Logger.Log($"disable mod {MOD_NAME}");
            }
            catch (Exception e)
            {
                Logger.LogError($"禁用mod失败: {e}");
            }
        }

        //下面两个函数需要实现，实现后的效果是：ModSetting和mod之间不需要启动顺序，两者无论谁先启动都能正常添加设置
        private void ModManager_OnModActivated(ModInfo arg1, Duckov.Modding.ModBehaviour arg2)
        {
            //(触发时机:此mod在ModSetting之前启用)检查启用的mod是否是ModSetting,是进行初始化
            if (arg1.name != Api.ModSettingAPI.MOD_NAME || !Api.ModSettingAPI.Init(info)) return;
            ModSettingManager.needUpdate = true;
        }

        protected override void OnAfterSetup()
        {
            //(触发时机:此mod在ModSetting之后启用)此mod，Setup后,尝试进行初始化
            if (Api.ModSettingAPI.Init(info))
            {
                ModSettingManager.needUpdate = true;
            }
        }

        void Update()
        {
            try
            {
                // if (ModSettingManager.needUpdate) ModSettingManager.Update();
                CustomMinimapManager.Update();
                CustomMinimapManager.CheckToggleKey();
                //PoiManager.Update();
            }
            catch (Exception e)
            {
                Logger.LogError($"更新失败: {e}");
            }
        }
    }
}
