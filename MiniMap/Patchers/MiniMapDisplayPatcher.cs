using Duckov.MiniMaps;
using Duckov.MiniMaps.UI;
using Duckov.Utilities;
using MiniMap.Managers;
using MiniMap.Poi;
using MiniMap.Utils;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using ZoinkModdingLibrary.Attributes;
using ZoinkModdingLibrary.Patcher;

namespace MiniMap.Patchers
{
    [TypePatcher(typeof(MiniMapDisplay))]
    public class MiniMapDisplayPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new MiniMapDisplayPatcher();

        private MiniMapDisplayPatcher() { }

        [MethodPatcher("HandlePointOfInterest", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool HandlePointOfInterestPrefix(MiniMapDisplay __instance, MonoBehaviour poi)
        {
            if (poi == null) return false;
            if (poi is CharacterPoi characterPoi)
            {
                CharacterPoiManager.HandlePointOfInterest(characterPoi, __instance == CustomMinimapManager.OriginalMinimapDisplay);
                return false;
            }
            return true;
        }

        [MethodPatcher("ReleasePointOfInterest", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool ReleasePointOfInterestPrefix(MiniMapDisplay __instance, MonoBehaviour poi)
        {
            if (poi == null) return false;
            if (poi is CharacterPoi characterPoi)
            {
                CharacterPoiManager.ReleasePointOfInterest(characterPoi, __instance == CustomMinimapManager.OriginalMinimapDisplay);
                return false;
            }
            return true;
        }

        [MethodPatcher("HandlePointsOfInterests", PatchType.Postfix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void HandlePointsOfInterestsPrefix(MiniMapDisplay __instance)
        {
            CharacterPoiManager.HandlePointsOfInterests(__instance == CustomMinimapManager.OriginalMinimapDisplay);
        }

        [MethodPatcher("SetupRotation", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool SetupRotationPrefix(MiniMapDisplay __instance)
        {
            try
            {
                float rotationAngle = ModSettingManager.GetValue<bool>("mapRotation") ? MiniMapCommon.GetMinimapRotation() : MiniMapCommon.originMapZRotation;
                __instance.transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);
                return false;
            }
            catch (Exception e)
            {
                ModBehaviour.Logger.LogError($"设置小地图旋转时出错：" + e.ToString());
                return true;
            }
        }

        [MethodPatcher("RegisterEvents", PatchType.Postfix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void RegisterEventsPostfix(MiniMapDisplay __instance)
        {
            AssemblyOption.BindStaticEvent(typeof(CharacterPoiManager), nameof(CharacterPoiManager.PoiRegistered), __instance, "HandlePointOfInterest");
            AssemblyOption.BindStaticEvent(typeof(CharacterPoiManager), nameof(CharacterPoiManager.PoiUnregistered), __instance, "ReleasePointOfInterest");
        }

        [MethodPatcher("UnregisterEvents", PatchType.Postfix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void UnregisterEventsPostfix(MiniMapDisplay __instance)
        {
            AssemblyOption.UnbindStaticEvent(typeof(CharacterPoiManager), nameof(CharacterPoiManager.PoiRegistered), __instance, "HandlePointOfInterest");
            AssemblyOption.UnbindStaticEvent(typeof(CharacterPoiManager), nameof(CharacterPoiManager.PoiUnregistered), __instance, "ReleasePointOfInterest");
        }
    }
}