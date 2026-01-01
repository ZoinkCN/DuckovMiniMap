using Duckov.MiniMaps;
using MiniMap.Extentions;
using MiniMap.Managers;
using MiniMap.Poi;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MiniMap.Utils
{
    public static class PoiCommon
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
                    return ModConfig.LoadSprite(iconName);
                }
            }
            scale = defaultScale;
            isBoss = false;
            return ModConfig.LoadSprite(defaultIconName);
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
                    ModBehaviour.Logger.Log($"Setting Up characterPoi for {(character.IsMainCharacter ? "Main Character" : preset.DisplayName)}");
                    JObject? iconConfig = ModConfig.LoadConfig("iconConfig.json");
                    Sprite? icon = GetIcon(iconConfig, preset.name, out scaleFactor, out bool isBoss);
                    pointOfInterest.Setup(icon, character, cachedName: preset.nameKey, followActiveScene: true);
                    pointOfInterest.ScaleFactor = scaleFactor;
                }, () =>
                {
                    ModBehaviour.Logger.Log($"Handling Points Of Interests");
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
                    ModBehaviour.Logger.Log($"Setting Up directionPoi for {(character.IsMainCharacter ? "Main Character" : preset?.DisplayName)}");
                    Sprite? icon = ModConfig.LoadSprite("CharactorDirection.png");
                    pointOfInterest.BaseEulerAngle = 45f;
                    pointOfInterest.Setup(icon, character: character, cachedName: preset?.DisplayName, followActiveScene: true);
                    pointOfInterest.ScaleFactor = scaleFactor;
                }, () =>
                {
                    ModBehaviour.Logger.Log($"Handling Points Of Interests");
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
            ModBehaviour.Logger.Log($"Finished Loading Scene: {obj.sceneName}");
        }
    }

    //public class BossCharacterBehaviour : MonoBehaviour
    //{
    //    private void Update()
    //    {
    //        if (enabled && !gameObject.activeSelf)
    //        {
    //            gameObject.SetActive(true);
    //        }
    //    }
    //}
}
