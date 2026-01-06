using Duckov.MiniMaps;
using MiniMap.Extentions;
using MiniMap.Managers;
using MiniMap.Poi;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ZoinkModdingLibrary.Utils;

namespace MiniMap.Utils
{
    public static class PoiCommon
    {
        private static Sprite? GetIcon(JObject? config, string presetName, out float scale, out CharacterType characterType)
        {
            if (config == null)
            {
                scale = 0.5f;
                characterType = CharacterType.Enemy;
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
                    if(presetName == "PetPreset_NormalPet")
                    {
                        characterType = CharacterType.Pet;
                    }
                    else
                    {
                        characterType = item.Key switch
                        {
                            "friendly" => CharacterType.NPC,
                            "neutral" => CharacterType.Neutral,
                            "boss" => CharacterType.Boss,
                            _ => CharacterType.Enemy,
                        };
                    }
                    return ModFileOperations.LoadSprite(iconName);
                }
            }
            scale = defaultScale;
            characterType = CharacterType.Enemy;
            return ModFileOperations.LoadSprite(defaultIconName);
        }

        public static void CreatePoiIfNeeded(CharacterMainControl? character, out IPointOfInterest? characterPoi, out IPointOfInterest? directionPoi, PoiShows? poiShows = null)
        {
            if (!LevelManager.LevelInited || character == null)
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
            CharacterType characterType;
            if (characterPoi == null)
            {
                CharacterRandomPreset? preset = character.characterPreset;
                if (preset == null)
                {
                    return;
                }
                characterPoi = poiObject.AddComponent<CharacterPointOfInterest>();
                CharacterPointOfInterest pointOfInterest = (CharacterPointOfInterest)characterPoi;
                ModBehaviour.Logger.Log($"Setting Up characterPoi for {(character.IsMainCharacter ? "Main Character" : preset.DisplayName)}");
                JObject? iconConfig = ModFileOperations.LoadJson("iconConfig.json", ModBehaviour.Logger);
                Sprite? icon = GetIcon(iconConfig, preset.name, out scaleFactor, out characterType);
                pointOfInterest.Setup(icon, character, poiShows, cachedName: preset.nameKey, followActiveScene: true);
                pointOfInterest.ScaleFactor = scaleFactor;
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
                ModBehaviour.Logger.Log($"Setting Up directionPoi for {(character.IsMainCharacter ? "Main Character" : preset?.DisplayName)}");
                Sprite? icon = ModFileOperations.LoadSprite("CharactorDirection.png");
                pointOfInterest.BaseEulerAngle = 45f;
                pointOfInterest.Setup(icon, character, poiShows, cachedName: preset?.DisplayName, followActiveScene: true);
                pointOfInterest.ScaleFactor = scaleFactor;
            }
        }

        public static bool IsDead(CharacterMainControl? character)
        {
            return !(character != null && character.Health && !character.Health.IsDead);
        }
    }
}
