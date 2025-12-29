using Duckov.MiniMaps;
using Duckov.Scenes;
using MiniMap.Extentions;
using Newtonsoft.Json.Linq;
using SodaCraft.Localizations;
using UnityEngine;

namespace MiniMap.Managers
{
    public static class CharacterPoiCommon
    {
        private static Sprite? GetIcon(JObject? config, string presetName, out float scale, out bool isBoss)
        {
            if (config == null)
            {
                scale = 0.5f;
                isBoss = false;
                return null;
            }
            float defaultScale = config.Value<float?>("defaultScale") ?? 1f;
            string? defaultIconName = config.Value<string?>("defaultIcon");
            foreach (KeyValuePair<string, JToken?> item in config)
            {
                if (item.Value is not JObject jObject) { continue; }
                if (jObject.ContainsKey(presetName))
                {
                    string? iconName = jObject.Value<string?>(presetName);
                    if (string.IsNullOrEmpty(iconName))
                    {
                        iconName = jObject.Value<string?>("defaultIcon");
                    }
                    if (string.IsNullOrEmpty(iconName))
                    {
                        iconName = defaultIconName;
                    }
                    scale = jObject.Value<float?>("scale") ?? defaultScale;
                    isBoss = item.Key.ToLower() == "boss";
                    return Util.LoadSprite(iconName);
                }
            }
            scale = defaultScale;
            isBoss = false;
            return Util.LoadSprite(defaultIconName);
        }

        public static void CreatePoiIfNeeded(CharacterMainControl character, out IPointOfInterest? characterPoi, out IPointOfInterest? directionPoi)
        {
            if (!LevelManager.LevelInited)
            {
                characterPoi = null;
                directionPoi = null;
                return;
            }

            GameObject poiObject = character.gameObject;
            if (poiObject == null)
            {
                characterPoi = null;
                directionPoi = null;
                return;
            }
            if (character.transform.parent?.name == "Level_Factory_Main")
            {
                if (poiObject != null)
                {
                    GameObject.Destroy(poiObject);
                }
                characterPoi = null;
                directionPoi = null;
                return;
            }
            float scaleFactor = 1;
            directionPoi = poiObject.GetComponent<DirectionPointOfInterest>();
            characterPoi = poiObject.GetComponent<SimplePointOfInterest>();
            characterPoi ??= poiObject.GetComponent<CharacterPointOfInterest>();
            if (characterPoi == null)
            {
                CharacterRandomPreset? preset = character.characterPreset;
                if (preset == null)
                {
                    return;
                }
                characterPoi = poiObject.AddComponent<CharacterPointOfInterest>();
                CharacterPointOfInterest pointOfInterest = (CharacterPointOfInterest)characterPoi;
                ModBehaviour.Instance?.ExecuteWithDebounce(() =>
                {
                    Debug.Log($"[MiniMap] Setting Up characterPoi for {(character.IsMainCharacter ? "Main Character" : preset.DisplayName)}");
                    JObject? iconConfig = Util.LoadConfig("iconConfig.json");
                    Sprite? icon = GetIcon(iconConfig, preset.name, out scaleFactor, out bool isBoss);
                    pointOfInterest.Setup(icon, character, displayName: preset.nameKey, followActiveScene: true);
                    pointOfInterest.ScaleFactor = scaleFactor;
                }, () =>
                {
                    Debug.Log("[MiniMap] Handling Points Of Interests");
                    CustomMinimapManager.CallDisplayMethod("HandlePointsOfInterests");
                });
            }
            if (directionPoi == null)
            {
                CharacterRandomPreset? preset = character.characterPreset;
                if (preset == null && !character.IsMainCharacter)
                {
                    return;
                }
                directionPoi = poiObject.AddComponent<DirectionPointOfInterest>();
                DirectionPointOfInterest pointOfInterest = (DirectionPointOfInterest)directionPoi;
                ModBehaviour.Instance?.ExecuteWithDebounce(() =>
                {
                    Debug.Log($"[MiniMap] Setting Up directionPoi for {(character.IsMainCharacter ? "Main Character" : preset?.DisplayName)}");
                    Sprite? icon = Util.LoadSprite("CharactorDirection.png");
                    pointOfInterest.BaseEulerAngle = 45f;
                    pointOfInterest.Setup(icon, character: character, tagName: preset?.DisplayName, followActiveScene: true);
                    pointOfInterest.ScaleFactor = scaleFactor;
                }, () =>
                {
                    Debug.Log("[MiniMap] Handling Points Of Interests");
                    CustomMinimapManager.CallDisplayMethod("HandlePointsOfInterests");
                });
            }
        }

        public static bool IsDead(CharacterMainControl? character)
        {
            return !(character != null && character.Health && !character.Health.IsDead);
        }

        public static void OnFinishedLoadingScene(SceneLoadingContext obj)
        {
            Debug.Log($"[MiniMap] Finished Loading Scene: {obj.sceneName}");
        }
    }

    public class CharacterPointOfInterest : MonoBehaviour, IPointOfInterest
    {
        private Sprite? icon;
        private CharacterMainControl? character;
        private int characterID;
        private bool localized = true;
        private string? displayName = "";
        private bool followActiveScene;
        private bool isArea;
        private float areaRadius;
        private float scaleFactor = 1f;
        private bool hideIcon;
        private string? overrideSceneID;

        public CharacterMainControl? Character => character;
        public int CharacterID => characterID;
        public float ScaleFactor { get => scaleFactor; set => scaleFactor = value; }
        public Color ShadowColor => Color.clear;
        public float ShadowDistance => 0f;
        public bool Localized { get => localized; set => localized = value; }
        public string? DisplayName => localized ? displayName.ToPlainText() : displayName;
        public Sprite? Icon => icon;
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
        public bool HideIcon { get => hideIcon; set => hideIcon = value; }

        private void OnEnable()
        {
            PointsOfInterests.Register(this);
        }

        private void OnDisable()
        {
            PointsOfInterests.Unregister(this);
        }

        public void Setup(Sprite? icon, CharacterMainControl character, string? displayName = null, bool followActiveScene = false, string? overrideSceneID = null)
        {
            this.character = character;
            this.characterID = character.GetInstanceID();
            this.icon = icon;
            this.displayName = displayName;
            this.followActiveScene = followActiveScene;
            this.overrideSceneID = overrideSceneID;
            PointsOfInterests.Unregister(this);
            PointsOfInterests.Register(this);
        }

        private void Update()
        {
            if (CharacterPoiCommon.IsDead(character))
            {
                Destroy(this.gameObject);
                return;
            }
        }
    }

    public class DirectionPointOfInterest : MonoBehaviour, IPointOfInterest
    {
        private Sprite? icon;
        private float rotationEulerAngle;
        private float baseEulerAngle;
        private string? tagName;
        private CharacterMainControl? character;
        private int characterID;
        private bool followActiveScene;
        private bool isArea;
        private float areaRadius;
        private float scaleFactor = 1f;
        private bool hideIcon;
        private string? overrideSceneID;

        public CharacterMainControl? Character => character;
        public int CharacterID => characterID;

        public float RotationEulerAngle { get => rotationEulerAngle % 360; set => rotationEulerAngle = value % 360; }
        public float BaseEulerAngle { get => baseEulerAngle % 360; set => baseEulerAngle = value % 360; }
        public float RealEulerAngle => (baseEulerAngle + rotationEulerAngle) % 360;
        public string DisplayName => string.Empty;
        public string? TagName => tagName;
        public float ScaleFactor { get => scaleFactor; set => scaleFactor = value; }
        public Color ShadowColor => Color.clear;
        public float ShadowDistance => 0f;
        public Sprite? Icon => icon;
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
        public bool HideIcon { get => hideIcon; set => hideIcon = value; }

        private void OnEnable()
        {
            PointsOfInterests.Register(this);
        }

        private void OnDisable()
        {
            PointsOfInterests.Unregister(this);
        }

        public void Setup(Sprite? icon, CharacterMainControl character, string? tagName = null, bool followActiveScene = false, string? overrideSceneID = null)
        {
            this.icon = icon;
            this.tagName = tagName;
            this.character = character;
            this.characterID = character.GetInstanceID();
            this.followActiveScene = followActiveScene;
            this.overrideSceneID = overrideSceneID;
            PointsOfInterests.Unregister(this);
            PointsOfInterests.Register(this);
        }

        private void Update()
        {
            if (CharacterPoiCommon.IsDead(character))
            {
                Destroy(this.gameObject);
                return;
            }
            if (character!.IsMainCharacter)
            {
                RotationEulerAngle = MiniMapCommon.GetPlayerMinimapRotation().eulerAngles.z;
            }
            else
            {
                RotationEulerAngle = MiniMapCommon.GetPlayerMinimapRotation(character.movementControl.targetAimDirection).eulerAngles.z;
            }
        }
    }

    public class BossCharacterBehaviour : MonoBehaviour
    {
        private void Update()
        {
            if (enabled && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }
    }
}
