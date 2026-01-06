using Duckov.MiniMaps;
using Duckov.Scenes;
using MiniMap.Extentions;
using MiniMap.Managers;
using MiniMap.Utils;
using SodaCraft.Localizations;
using Unity.VisualScripting;
using UnityEngine;

namespace MiniMap.Poi
{
    public abstract class CharacterPointOfInterestBase : MonoBehaviour, IPointOfInterest
    {
        private bool initialized = false;
        private bool isMain = false;
        private bool isPet = false;

        private CharacterMainControl? character;
        private CharacterType characterType;
        private string? cachedName;
        private bool showInMap;
        private bool showInMiniMap;
        private bool showOnlyAcivated;
        private Sprite? icon;
        private Color color = Color.white;
        private bool localized = true;
        private bool followActiveScene;
        private bool isArea;
        private float areaRadius;
        private float scaleFactor = 1f;
        private bool hideIcon = false;
        private string? overrideSceneID;

        public virtual CharacterMainControl? Character => character;
        public virtual CharacterType CharacterType => characterType;
        public virtual string? CachedName { get => cachedName; set => cachedName = value; }
        public virtual bool ShowOnlyAcivated
        {
            get => showOnlyAcivated;
            set
            {
                if (showOnlyAcivated != value)
                {
                    showOnlyAcivated = value;
                    if (value)
                    {
                        if (!gameObject.activeSelf)
                        {
                            Unregister();
                        }
                    }
                    else
                    {
                        Register();
                    }
                }
            }
        }
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
        public virtual Color Color { get => color; set => color = value; }
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
            Register();
        }

        protected virtual void OnDisable()
        {
            if (ShowOnlyAcivated)
            {
                Unregister();
            }
        }

        public virtual void Setup(Sprite? icon, CharacterMainControl character, PoiShows? poiShows, string? cachedName = null, bool followActiveScene = false, string? overrideSceneID = null)
        {
            if (initialized) return;
            this.character = character;
            this.icon = icon;
            this.CachedName = cachedName;
            this.followActiveScene = followActiveScene;
            this.overrideSceneID = overrideSceneID;
            if (character.IsMainCharacter)
            {
                isMain = true;
            }
            else
            {
                CharacterRandomPreset? preset = Character?.characterPreset;
                isPet = preset?.name == "PetPreset_NormalPet";
            }
            ModSettingManager.ConfigChanged += OnConfigChanged;
            SetShows(poiShows);
            initialized = true;
        }

        private void OnConfigChanged(string key, object? value)
        {
            if (value == null) return;
            switch (key)
            {
                case "showOnlyActivated":
                    ShowOnlyAcivated = (bool)value;
                    break;
                case "showPoiInMap":
                    ShowInMap = (bool)value;
                    break;
                case "showPoiInMiniMap":
                    ShowInMiniMap = (bool)value;
                    break;
                case "showPetPoi":
                    if (isPet)
                    {
                        ShowInMap = ShowInMiniMap = (bool)value;
                    }
                    break;
            }
        }

        protected virtual void Update()
        {
            if (character != null && !isMain && PoiCommon.IsDead(character))
            {
                Destroy(this.gameObject);
                return;
            }
            //if (isMain)
            //{
            //    ShowInMap = ShowInMiniMap = true;
            //}

        }

        public virtual void Register(bool force = false)
        {
            if (force)
            {
                PointsOfInterests.Unregister(this);
            }
            if (!PointsOfInterests.Points.Contains(this))
            {
                ModBehaviour.Instance?.ExecuteWithDebounce(() =>
                {
                    PointsOfInterests.Register(this);
                }, () =>
                {
                    //ModBehaviour.Logger.Log($"Handling Points Of Interests");
                    //CustomMinimapManager.CallDisplayMethod("HandlePointsOfInterests");
                });
            }
        }

        public virtual void Unregister()
        {
            ModBehaviour.Instance?.ExecuteWithDebounce(() =>
                {
                    PointsOfInterests.Unregister(this);
                }, () =>
                {
                    //ModBehaviour.Logger.Log($"Handling Points Of Interests");
                    //CustomMinimapManager.CallDisplayMethod("HandlePointsOfInterests");
                });
        }

        public virtual void SetShows(PoiShows? poiShows)
        {
            bool showOnlyActivated = poiShows?.ShowOnlyActivated ?? ModSettingManager.GetValue("showOnlyActivated", false);
            bool showPetPoi = poiShows?.ShowPetPoi ?? ModSettingManager.GetValue("showPetPoi", true);
            bool showInMap = poiShows?.ShowInMap ?? ModSettingManager.GetValue("showPoiInMap", true);
            bool showInMiniMap = poiShows?.ShowInMiniMap ?? ModSettingManager.GetValue("showPoiInMiniMap", true);
            ShowOnlyAcivated = showOnlyActivated;
            if (isPet)
            {
                ShowInMap = ShowInMiniMap = showPetPoi;
            }
            else
            {
                ShowInMap = showInMap || isMain;
                ShowInMiniMap = showInMiniMap || isMain;
            }
            if (ShowOnlyAcivated && !(Character?.gameObject.activeSelf ?? false))
            {
                Unregister();
            }
            else
            {
                Register();
            }
        }
    }
}
