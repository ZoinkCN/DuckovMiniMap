using Duckov.MiniMaps;
using Duckov.Scenes;
using MiniMap.Extentions;
using MiniMap.Managers;
using MiniMap.Utils;
using SodaCraft.Localizations;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

namespace MiniMap.Poi
{
    /// <summary>
    /// 角色兴趣点基类
    /// 注意：移除了Update方法，死亡检查将使用事件系统处理（后续实现）
    /// </summary>
    public abstract class CharacterPointOfInterestBase : MonoBehaviour, IPointOfInterest
    {
        private bool initialized = false;

        private CharacterMainControl? character;
        private CharacterType characterType;
        private string? cachedName;
        private bool showOnlyActivated;
        private Sprite? icon;
        private Color color = Color.white;
        private Color shadowColor = Color.clear;
        private float shadowDistance = 0f;
        private bool localized = true;
        private bool followActiveScene;
        private bool isArea;
        private float areaRadius;
        private float scaleFactor = 1f;
        private bool hideIcon = false;
        private string? overrideSceneID;

        public virtual bool Initialized => initialized;
        public virtual CharacterMainControl? Character => character;
        public virtual CharacterType CharacterType => characterType;
        public virtual string? CachedName { get => cachedName; set => cachedName = value; }
        public virtual bool ShowOnlyActivated
        {
            get => showOnlyActivated;
            protected set
            {
                showOnlyActivated = value;
                if (value && !(character?.gameObject.activeSelf ?? false))
                {
                    Unregister();
                }
                else
                {
                    Register();
                }
            }
        }
        public virtual string DisplayName => CachedName?.ToPlainText() ?? string.Empty;
        public virtual float ScaleFactor { get => scaleFactor; set => scaleFactor = value; }
        public virtual Color Color { get => color; set => color = value; }
        public virtual Color ShadowColor { get => shadowColor; set => shadowColor = value; }
        public virtual float ShadowDistance { get => shadowDistance; set => shadowDistance = value; }
        public virtual bool Localized { get => localized; set => localized = value; }
        public virtual Sprite? Icon => icon;
        public virtual int OverrideScene
        {
            get
            {
                if (followActiveScene && MultiSceneCore.ActiveSubScene.HasValue)
                {
                    return MultiSceneCore.ActiveSubScene.Value.buildIndex;
                }

                if (!string.IsNullOrEmpty(overrideSceneID))
                {
                    System.Collections.Generic.List<SceneInfoEntry>? entries = SceneInfoCollection.Entries;
                    SceneInfoEntry? sceneInfo = entries?.Find(e => e.ID == overrideSceneID);
                    return sceneInfo?.BuildIndex ?? -1;
                }
                return -1;
            }
        }
        public virtual bool IsArea { get => isArea; set => isArea = value; }
        public virtual float AreaRadius { get => areaRadius; set => areaRadius = value; }
        public virtual bool HideIcon { get => hideIcon; set => hideIcon = value; }
        
        /// <summary>
        /// 启用时注册兴趣点
        /// </summary>
        protected virtual void OnEnable()
        {
            Register();
        }

        /// <summary>
        /// 禁用时注销兴趣点
        /// </summary>
        protected virtual void OnDisable()
        {
            if (ShowOnlyActivated)
            {
                Unregister();
            }
        }

        /// <summary>
        /// 设置兴趣点（使用精灵图标）
        /// </summary>
        public virtual void Setup(Sprite? icon, CharacterMainControl character, CharacterType characterType, string? cachedName = null, bool followActiveScene = false, string? overrideSceneID = null)
        {
            if (initialized)
            {
                return;
            }
            
            this.character = character;
            this.characterType = characterType;
            this.icon = icon;
            this.cachedName = cachedName;
            this.followActiveScene = followActiveScene;
            this.overrideSceneID = overrideSceneID;
            ShowOnlyActivated = ModSettingManager.GetValue("showOnlyActivated", false);
            ModSettingManager.ConfigChanged += OnConfigChanged;
            initialized = true;
        }

        /// <summary>
        /// 设置兴趣点（复制现有兴趣点）
        /// </summary>
        public virtual void Setup(SimplePointOfInterest poi, CharacterMainControl character, CharacterType characterType, bool followActiveScene = false, string? overrideSceneID = null)
        {
            if (initialized)
            {
                return;
            }
            
            this.character = character;
            this.characterType = characterType;
            this.icon = GameObject.Instantiate(poi.Icon);
            FieldInfo? field = typeof(SimplePointOfInterest).GetField("displayName", BindingFlags.NonPublic | BindingFlags.Instance);
            this.cachedName = field.GetValue(poi) as string;
            this.followActiveScene = followActiveScene;
            this.overrideSceneID = overrideSceneID;
            this.isArea = poi.IsArea;
            this.areaRadius = poi.AreaRadius;
            this.color = poi.Color;
            this.shadowColor = poi.ShadowColor;
            this.shadowDistance = poi.ShadowDistance;
            ShowOnlyActivated = ModSettingManager.GetValue("showOnlyActivated", false);
            ModSettingManager.ConfigChanged += OnConfigChanged;
            initialized = true;
        }

        /// <summary>
        /// 配置变更事件处理
        /// </summary>
        private void OnConfigChanged(string key, object? value)
        {
            if (value == null)
            {
                return;
            }
            
            switch (key)
            {
                case "showOnlyActivated":
                    ShowOnlyActivated = (bool)value;
                    break;
                case "showPoiInMiniMap":
                case "showPetPoi":
                case "showBossPoi":
                case "showEnemyPoi":
                case "showNeutralPoi":
                    ModBehaviour.Instance?.ExecuteWithDebounce(() =>
                    {
                        // 空操作，仅用于防抖
                    }, () =>
                    {
                        CustomMinimapManager.CallDisplayMethod("HandlePointsOfInterests");
                    });
                    break;
            }
        }

        /// <summary>
        /// 注意：移除了Update方法
        /// 原因：
        /// 1. 死亡检查将使用事件系统处理（后续实现）
        /// 2. 方向更新由DistanceBasedUpdateManager异步处理
        /// 3. 减少每帧的CPU开销，提升性能
        /// </summary>

        /// <summary>
        /// 销毁时清理事件订阅
        /// </summary>
        protected void OnDestroy()
        {
            ModSettingManager.ConfigChanged -= OnConfigChanged;
        }

        /// <summary>
        /// 注册兴趣点
        /// </summary>
        public virtual void Register(bool force = false)
        {
            if (force)
            {
                PointsOfInterests.Unregister(this);
            }
            if (!PointsOfInterests.Points.Contains(this))
            {
                PointsOfInterests.Register(this);
            }
        }

        /// <summary>
        /// 注销兴趣点
        /// </summary>
        public virtual void Unregister()
        {
            PointsOfInterests.Unregister(this);
        }

        /// <summary>
        /// 判断是否在小地图中显示
        /// </summary>
        public virtual bool WillShow(bool isOriginalMap = true)
        {
            bool willShowInThisMap = isOriginalMap ? ModSettingManager.GetValue("showPoiInMap", true) : ModSettingManager.GetValue("showPoiInMiniMap", true);
            return characterType switch
            {
                CharacterType.Main or CharacterType.NPC => true,
                CharacterType.Pet => ModSettingManager.GetValue("showPetPoi", true),
                CharacterType.Boss => ModSettingManager.GetValue("showBossPoi", true) && willShowInThisMap,
                CharacterType.Enemy => ModSettingManager.GetValue("showEnemyPoi", true) && willShowInThisMap,
                CharacterType.Neutral => ModSettingManager.GetValue("showNeutralPoi", true) && willShowInThisMap,
                _ => false,
            };
        }
    }
}