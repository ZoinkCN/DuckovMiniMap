using Duckov.MiniMaps;
using Duckov.MiniMaps.UI;
using HarmonyLib;
using MiniMap.Managers;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMap.Extenders
{
    [HarmonyPatch(typeof(CharacterSpawnerRoot))]
    [HarmonyPatch("AddCreatedCharacter")]
    public static class CharacterSpawnerRootAddCharacterExtender
    {
        public static bool Prefix(CharacterMainControl c)
        {
            try
            {
                CharacterPoiCommon.CreatePoiIfNeeded(c, out _, out _);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniMap] characterPoi add failed: {e.Message}");
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl))]
    [HarmonyPatch("Update")]
    public static class CharacterMainControlUpdateExtender
    {
        public static void Postfix(CharacterMainControl __instance)
        {
            try
            {
                CharacterPoiCommon.CreatePoiIfNeeded(__instance, out _, out _);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniMap] characterPoi update failed: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(PointOfInterestEntry))]
    [HarmonyPatch("UpdateRotation")]
    public static class PointOfInterestEntryUpdateRotationExtender
    {
        public static bool Prefix(PointOfInterestEntry __instance, MiniMapDisplayEntry ___minimapEntry)
        {
            try
            {
                if (__instance.Target is DirectionPointOfInterest poi)
                {
                    MiniMapDisplay? display = ___minimapEntry.GetComponentInParent<MiniMapDisplay>();
                    if (display == null)
                    {
                        return true;
                    }
                    __instance.transform.rotation = Quaternion.Euler(0f, 0f, poi.RealEulerAngle + display.transform.rotation.eulerAngles.z);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniMap] PointOfInterestEntry UpdateRotation failed: {e.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(PointOfInterestEntry))]
    [HarmonyPatch("Update")]
    public static class PointOfInterestEntryUpdateExtender
    {
        public static bool Prefix(PointOfInterestEntry __instance, Image ___icon)
        {
            var character = __instance.Target as CharacterMainControl;
            if (__instance.Target == null || character != null && CharacterPoiCommon.IsDead(character))
            {
                GameObject.Destroy(__instance.gameObject);
                return false;
            }
            if (character?.IsMainCharacter ?? false)
            {
                var parent = __instance.transform.parent;
                if (parent != null && parent.GetChild(parent.childCount - 1) != __instance)
                {
                    __instance.transform.SetAsLastSibling();
                    Debug.Log("[MiniMap] Move main character POI entry to top");
                }
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
    }
}
