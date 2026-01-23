using Duckov.MiniMaps;
using MiniMap.Extentions;
using MiniMap.Managers;
using MiniMap.Poi;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using ZoinkModdingLibrary.Utils;

namespace MiniMap.Utils
{
    public static class CharacterPoiCommon
    {
        private static CharacterPoiIconData GetIcon(JObject? config, CharacterMainControl? character, out CharacterType characterType)
        {
            CharacterPoiIconData iconData = new CharacterPoiIconData();
            characterType = CharacterType.Enemy;
            if (config == null)
            {
                return iconData;
            }
            CharacterRandomPreset? preset = character?.characterPreset;
            float overallDefaultIconScale = config.Value<float?>("defaultIconScale") ?? 1f;
            float overallDefaultArrowScale = config.Value<float?>("defaultArrowScale") ?? 1f;
            string? overallDefaultIconName = config.Value<string?>("defaultIcon");
            string? overallDefaultArrowName = config.Value<string?>("defaultArrow");
            if (character.IsMainCharacter())
            {
                JObject? data = config["main"] as JObject;
                string? iconName = data?.Value<string?>("icon");
                string? arrowName = data?.Value<string?>("arrow") ?? overallDefaultArrowName;
                iconData.Icon = ModFileOperations.LoadSprite(iconName);
                iconData.Arrow = ModFileOperations.LoadSprite(arrowName);
                iconData.IconScale = data?.Value<float?>("iconScale") ?? overallDefaultIconScale;
                iconData.ArrowScale = data?.Value<float?>("arrowScale") ?? overallDefaultArrowScale;
                characterType = CharacterType.Main;
                return iconData;
            }
            if (preset == null || string.IsNullOrEmpty(preset.name))
            {
                return iconData;
            }
            string presetName = preset.name;
            foreach (KeyValuePair<string, JToken?> item in config)
            {
                if (item.Key == "main" || item.Value is not JObject jObject) { continue; }
                float typeDefaultIconScale = jObject.Value<float?>("defaultIconScale") ?? overallDefaultIconScale;
                string? typeDefaultIconName = jObject.Value<string?>("defaultIcon") ?? overallDefaultIconName;
                float typeDefaultArrowScale = jObject.Value<float?>("defaultArrowScale") ?? overallDefaultArrowScale;
                string? typeDefaultArrowName = jObject.Value<string?>("defaultArrow") ?? overallDefaultArrowName;
                if (jObject.ContainsKey(presetName))
                {
                    JObject? data = jObject[presetName] as JObject;
                    string? iconName = data?.Value<string?>("icon") ?? typeDefaultIconName;
                    string? arrowName = data?.Value<string?>("arrow") ?? typeDefaultArrowName;
                    iconData.Icon = ModFileOperations.LoadSprite(iconName);
                    iconData.Arrow = ModFileOperations.LoadSprite(arrowName);
                    iconData.IconScale = data?.Value<float?>("iconScale") ?? typeDefaultIconScale;
                    iconData.ArrowScale = data?.Value<float?>("arrowScale") ?? typeDefaultArrowScale;
                    iconData.HideIcon = data?.Value<bool>("hideIcon") ?? false;
                    iconData.HideArrow = data?.Value<bool>("hideArrow") ?? false;

                    if (presetName == "PetPreset_NormalPet")
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
                    return iconData;
                }
            }
            iconData.Icon = ModFileOperations.LoadSprite(overallDefaultIconName);
            iconData.Arrow = ModFileOperations.LoadSprite(overallDefaultArrowName);
            iconData.IconScale = overallDefaultIconScale;
            iconData.ArrowScale = overallDefaultArrowScale;
            characterType = CharacterType.Enemy;
            return iconData;
        }

        public static void CreatePoiIfNeeded(CharacterMainControl? character, out CharacterPoi? characterPoi)
        {
            if (!LevelManager.LevelInited || character == null)
            {
                characterPoi = null;
                return;
            }
            if (character.transform.parent?.name == "Level_Factory_Main")
            {
                if (character.gameObject != null)
                {
                    GameObject.Destroy(character.gameObject);
                }
                characterPoi = null;
                return;
            }
            SimplePointOfInterest? originPoi = character.GetComponentInChildren<SimplePointOfInterest>() ?? character.GetComponent<SimplePointOfInterest>();
            GameObject poiObject = originPoi != null ? originPoi.gameObject : character.gameObject;
            if (poiObject == null)
            {
                characterPoi = null;
                return;
            }
            CharacterRandomPreset? preset = character.characterPreset;
            if (preset == null && !character.IsMainCharacter)
            {
                characterPoi = null;
                return;
            }
            characterPoi = poiObject.GetComponent<CharacterPoi>();
            //directionPoi = poiObject.GetOrAddComponent<DirectionPointOfInterest>();
            CharacterType characterType;
            if (characterPoi == null)
            {
                characterPoi = poiObject.AddComponent<CharacterPoi>();
                JObject? iconConfig = ModFileOperations.LoadJson("iconConfig.json", ModBehaviour.Logger);
                CharacterPoiIconData iconData = GetIcon(iconConfig, character, out characterType);
                Sprite? icon = iconData.Icon;
                Sprite? arrowIcon = iconData.Arrow;
                characterPoi.IconScaleFactor = iconData.IconScale;
                characterPoi.ArrowScaleFactor = iconData.ArrowScale;
                characterPoi.HideIcon = iconData.HideIcon;
                characterPoi.HideArrow = iconData.HideArrow;
                if (originPoi == null)
                {
                    characterPoi.Setup(iconData.Icon, iconData.Arrow, character, characterType, preset?.nameKey, followActiveScene: true);
                }
                else
                {
                    characterPoi.Setup(originPoi, iconData.Icon, arrowIcon, character, characterType, followActiveScene: true);
                }
                if (originPoi)
                {
                    GameObject.Destroy(originPoi);
                }
            }
            //if (!directionPoi.Initialized)
            //{
            //    
            //    directionPoi.BaseEulerAngle = 45f;
            //    directionPoi.ScaleFactor = scaleFactor;
            //    directionPoi.Setup(icon, character, characterType, cachedName: preset?.DisplayName, followActiveScene: true);
            //}
        }

        public static bool IsDead(CharacterMainControl? character)
        {
            return !(character != null && character.Health && !character.Health.IsDead);
        }
    }
}
