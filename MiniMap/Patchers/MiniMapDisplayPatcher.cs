using Duckov.MiniMaps.UI;
using Duckov.Utilities;
using MiniMap.Managers;
using MiniMap.Poi;
using MiniMap.Utils;
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
            if (poi is CharacterPointOfInterestBase characterPoi)
            {
                return (__instance == CustomMinimapManager.OriginalMinimapDisplay && characterPoi.ShowInMap)
                    || (__instance == CustomMinimapManager.DuplicatedMinimapDisplay && characterPoi.ShowInMiniMap);
            }
            return true;
        }

        [MethodPatcher("SetupRotation", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool SetupRotationPrefix(MiniMapDisplay __instance)
        {
            try
            {
                __instance.transform.rotation = ModSettingManager.GetValue<bool>("mapRotation")
                    ? MiniMapCommon.GetPlayerMinimapRotationInverse()
                    : Quaternion.Euler(0f, 0f, MiniMapCommon.originMapZRotation);
                return false;
            }
            catch (Exception e)
            {
                ModBehaviour.Logger.LogError($"设置小地图旋转时出错：" + e.ToString());
                return true;
            }
        }
    }
}