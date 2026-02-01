using Duckov.MiniMaps;
using Duckov.Modding;
using Duckov.Scenes;
using MiniMap.Managers;
using MiniMap.Utils;
using SodaCraft.Localizations;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ZoinkModdingLibrary.Extentions;
using ZoinkModdingLibrary.ModSettings;
using ZoinkModdingLibrary.Utils;

namespace MiniMap.Poi
{
    public class CharacterPoi : MonoBehaviour
    {
        private CharacterMainControl? character;
        private CharacterType characterType;
        private string? cachedName;
        private bool showOnlyActivated;
        private Sprite? icon;
        private Sprite? arrow;
        private Color color = Color.white;
        private Color arrowColor = Color.white;
        private Color shadowColor = Color.clear;
        private float shadowDistance = 0f;
        private bool localized = true;
        private bool followActiveScene;
        private bool isArea;
        private float areaRadius;
        private float iconScaleFactor = 1f;
        private float arrowScaleFactor = 1f;
        private bool hideIcon = false;
        private bool hideArrow = false;
        private string? overrideSceneID;
        private float rotationEulerAngle;
        //private float baseEulerAngle;

        public float RotationEulerAngle { get => rotationEulerAngle % 360; set => rotationEulerAngle = value % 360; }
        //public float BaseEulerAngle { get => baseEulerAngle % 360; set => baseEulerAngle = value % 360; }
        //public float RealEulerAngle => (baseEulerAngle + rotationEulerAngle) % 360;
        public CharacterMainControl? Character => character;
        public CharacterType CharacterType => characterType;
        public string? CachedName { get => cachedName; set => cachedName = value; }
        public bool ShowOnlyActivated
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
        public string DisplayName => CachedName?.ToPlainText() ?? string.Empty;
        public float IconScaleFactor { get => iconScaleFactor; set => iconScaleFactor = value; }
        public float ArrowScaleFactor { get => arrowScaleFactor; set => arrowScaleFactor = value; }
        public Color Color { get => color; set => color = value; }
        public Color ArrowColor { get => arrowColor; set => arrowColor = value; }
        public Color ShadowColor { get => shadowColor; set => shadowColor = value; }
        public float ShadowDistance { get => shadowDistance; set => shadowDistance = value; }
        public bool Localized { get => localized; set => localized = value; }
        public Sprite? Icon => icon;
        public Sprite? Arrow => arrow;
        public int OverrideScene
        {
            get
            {
                if (followActiveScene && MultiSceneCore.ActiveSubScene.HasValue)
                {
                    return MultiSceneCore.ActiveSubScene.Value.buildIndex;
                }

                if (!string.IsNullOrEmpty(overrideSceneID))
                {
                    List<SceneInfoEntry>? entries = SceneInfoCollection.Entries;
                    SceneInfoEntry? sceneInfo = entries?.Find(e => e.ID == overrideSceneID);
                    return sceneInfo?.BuildIndex ?? -1;
                }
                return -1;
            }
        }
        public bool IsArea { get => isArea; set => isArea = value; }
        public float AreaRadius { get => areaRadius; set => areaRadius = value; }
        public bool HideIcon { get => hideIcon || icon == null; set => hideIcon = value; }
        public bool HideArrow { get => hideArrow || arrow == null; set => hideArrow = value; }
        protected void OnEnable()
        {
            Register();
        }

        protected void OnDisable()
        {
            if (ShowOnlyActivated)
            {
                Unregister();
            }
        }

        public void Setup(Sprite? icon, Sprite? arrow, CharacterMainControl character, CharacterType characterType, string? cachedName = null, bool followActiveScene = false, string? overrideSceneID = null)
        {
            this.character = character;
            this.characterType = characterType;
            this.icon = icon;
            this.arrow = arrow;
            this.cachedName = cachedName;
            this.followActiveScene = followActiveScene;
            this.overrideSceneID = overrideSceneID;
            ShowOnlyActivated = ModSettingManager.GetValue(ModBehaviour.ModInfo, "showOnlyActivated", false);
            ModSettingManager.ConfigChanged += OnConfigChanged;
        }

        public void Setup(SimplePointOfInterest poi, Sprite? icon, Sprite? arrow, CharacterMainControl character, CharacterType characterType, bool followActiveScene = false, string? overrideSceneID = null)
        {
            this.character = character;
            this.characterType = characterType;
            this.icon = icon ?? GameObject.Instantiate(poi.Icon);
            this.arrow = arrow;
            FieldInfo? field = typeof(SimplePointOfInterest).GetField("displayName", BindingFlags.NonPublic | BindingFlags.Instance);
            this.cachedName = field.GetValue(poi) as string;
            this.followActiveScene = followActiveScene;
            this.overrideSceneID = overrideSceneID;
            this.isArea = poi.IsArea;
            this.areaRadius = poi.AreaRadius;
            this.color = poi.Color;
            this.shadowColor = poi.ShadowColor;
            this.shadowDistance = poi.ShadowDistance;
            ShowOnlyActivated = ModSettingManager.GetValue(ModBehaviour.ModInfo, "showOnlyActivated", false);
            ModSettingManager.ConfigChanged += OnConfigChanged;
        }

        private void OnConfigChanged(ModInfo modInfo, string key, object? value)
        {
            if (!modInfo.ModIdEquals(ModBehaviour.Instance!.info)) return;
            if (value == null) return;
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

                    }, () =>
                    {
                        MinimapManager.MinimapDisplay.InvokeMethod("HandlePointsOfInterests");
                    });
                    break;
            }
        }

        protected void Update()
        {
            if (character == null || characterType != CharacterType.Main && CharacterPoiCommon.IsDead(character))
            {
                Destroy(this.gameObject);
                return;
            }
            RotationEulerAngle = MiniMapCommon.GetChracterRotation(Character);
        }

        protected void OnDestroy()
        {
            Unregister();
            ModSettingManager.ConfigChanged -= OnConfigChanged;
        }

        public void Register(bool force = false)
        {
            if (force)
            {
                CharacterPoiManager.Unregister(this);
            }
            if (!CharacterPoiManager.Points.Contains(this))
            {
                CharacterPoiManager.Register(this);
            }
        }

        public void Unregister()
        {
            CharacterPoiManager.Unregister(this);
        }

        public bool WillShow(bool isOriginalMap = true)
        {
            bool willShowInThisMap = ModSettingManager.GetValue(ModBehaviour.ModInfo, isOriginalMap ? "showPoiInMap" : "showPoiInMiniMap", true);
            return characterType switch
            {
                CharacterType.Main or CharacterType.NPC => true,
                CharacterType.Pet => ModSettingManager.GetValue(ModBehaviour.ModInfo, "showPetPoi", true),
                CharacterType.Boss => ModSettingManager.GetValue(ModBehaviour.ModInfo, "showBossPoi", true) && willShowInThisMap,
                CharacterType.Enemy => ModSettingManager.GetValue(ModBehaviour.ModInfo, "showEnemyPoi", true) && willShowInThisMap,
                CharacterType.Neutral => ModSettingManager.GetValue(ModBehaviour.ModInfo, "showNeutralPoi", true) && willShowInThisMap,
                _ => false,
            };
        }
    }
}
