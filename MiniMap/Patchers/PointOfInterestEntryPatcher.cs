using Duckov.MiniMaps;
using Duckov.MiniMaps.UI;
using MiniMap.Managers;
using MiniMap.Poi;
using System;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using ZoinkModdingLibrary.Attributes;
using ZoinkModdingLibrary.ModSettings;
using ZoinkModdingLibrary.Patcher;
using ZoinkModdingLibrary.Utils;

namespace MiniMap.Patchers
{
    [TypePatcher(typeof(PointOfInterestEntry))]
    public class PointOfInterestEntryPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new PointOfInterestEntryPatcher();
        private PointOfInterestEntryPatcher() { }

        [MethodPatcher("Update", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool UpdatePrefix(PointOfInterestEntry __instance, Image ___icon, MiniMapDisplay ___master, TextMeshProUGUI ___displayName)
        {
            if (__instance.Target == null || __instance.Target.IsDestroyed())
            {
                __instance.gameObject.SetActive(false);
                return false;
            }
            if (__instance.Target is IPointOfInterest poi)
            {
                if (poi.Color != ___icon.color)
                {
                    ___icon.color = poi.Color;
                }
                ___displayName.text = ___master == MinimapManager.MinimapDisplay && ModSettingManager.GetValue(ModBehaviour.ModInfo, "hideDisplayName", false) ? "" : poi.DisplayName;
            }
            RectTransform icon = ___icon.rectTransform;
            RectTransform? layout = icon.parent as RectTransform;
            if (layout == null) { return true; }
            if (layout.localPosition + icon.localPosition != Vector3.zero)
            {
                layout.localPosition = Vector3.zero - icon.localPosition;
            }
            return true;
        }

        [MethodPatcher("Setup", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void SetupPrefix(PointOfInterestEntry __instance, MonoBehaviour target, Image ___icon)
        {
            if (target is IPointOfInterest poi)
            {
                VerticalLayoutGroup? layout = __instance.GetComponentInChildren<VerticalLayoutGroup>();
                if (layout == null) { return; }
                layout.transform.localPosition = poi.HideIcon || string.IsNullOrEmpty(poi.DisplayName) ? Vector3.zero : Vector3.zero - ___icon.transform.localPosition;
            }
        }
    }
}
