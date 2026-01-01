using Duckov.MiniMaps;
using Duckov.Scenes;
using MiniMap.Extentions;
using MiniMap.Managers;
using MiniMap.Utils;
using SodaCraft.Localizations;
using UnityEngine;

namespace MiniMap.Poi
{
    public abstract class CharacterPointOfInterestBase : MonoBehaviour, IPointOfInterest
    {
        private CharacterMainControl? character;
        private string? cachedName;
        private bool showInMap;
        private bool showInMiniMap;
        private Sprite? icon;
        private bool localized = true;
        private bool followActiveScene;
        private bool isArea;
        private float areaRadius;
        private float scaleFactor = 1f;
        private bool hideIcon = false;
        private string? overrideSceneID;

        public virtual CharacterMainControl? Character => character;
        public virtual string? CachedName { get => cachedName; set => cachedName = value; }
        public virtual bool ShowInMap { get => showInMap; set => showInMap = value; }
        public virtual bool ShowInMiniMap
        {
            get => showInMiniMap;
            set
            {
                if (value != showInMiniMap)
                {
                    ModBehaviour.Instance?.ExecuteWithDebounce(() =>
                        {
                            showInMiniMap = value;
                        }, () =>
                        {
                            CustomMinimapManager.CallDisplayMethod("HandlePointsOfInterests");
                        });
                }
            }
        }
        public virtual string DisplayName => CachedName?.ToPlainText() ?? string.Empty;
        public virtual float ScaleFactor { get => scaleFactor; set => scaleFactor = value; }
        public virtual Color ShadowColor => Color.clear;
        public virtual float ShadowDistance => 0f;
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
                    List<SceneInfoEntry>? entries = SceneInfoCollection.Entries;
                    SceneInfoEntry? sceneInfo = entries?.Find(e => e.ID == overrideSceneID);
                    return sceneInfo?.BuildIndex ?? -1;
                }
                return -1;
            }
        }
        public virtual bool IsArea { get => isArea; set => isArea = value; }
        public virtual float AreaRadius { get => areaRadius; set => areaRadius = value; }
        public virtual bool HideIcon { get => hideIcon; set => hideIcon = value; }
        protected virtual void OnEnable()
        {
            PointsOfInterests.Register(this);
        }

        protected virtual void OnDisable()
        {
            PointsOfInterests.Unregister(this);
        }

        public virtual void Setup(Sprite? icon, CharacterMainControl character, string? cachedName = null, bool followActiveScene = false, string? overrideSceneID = null)
        {
            this.character = character;
            this.icon = icon;
            this.CachedName = cachedName;
            this.followActiveScene = followActiveScene;
            this.overrideSceneID = overrideSceneID;
            PointsOfInterests.Unregister(this);
            PointsOfInterests.Register(this);
        }

        protected virtual void Update()
        {
            if (character != null && !character.IsMainCharacter && PoiCommon.IsDead(character))
            {
                Destroy(this.gameObject);
                return;
            }
            CharacterRandomPreset? preset = Character?.characterPreset;
            bool isMain = Character?.IsMainCharacter ?? false;
            if (preset?.name == "PetPreset_NormalPet")
            {
                ShowInMap = ShowInMiniMap = ModSettingManager.GetValue("showPetPoi", true);
            }
            else
            {
                ShowInMap = ModSettingManager.GetValue("showPoiInMap", true) || isMain;
                ShowInMiniMap = ModSettingManager.GetValue("showPoiInMiniMap", true) || isMain;
            }
        }
    }
}
