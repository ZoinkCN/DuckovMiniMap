using Duckov.MiniMaps.UI;
using UnityEngine.UI;
using ZoinkModdingLibrary.Attributes;
using ZoinkModdingLibrary.Patcher;

namespace MiniMap.Patchers
{
    [TypePatcher(typeof(MiniMapDisplayEntry))]
    public class MiniMapDisplayEntryPatcher : PatcherBase
    {
        public static new MiniMapDisplayEntryPatcher Instance { get; } = new MiniMapDisplayEntryPatcher();
        private MiniMapDisplayEntryPatcher() { }

        [MethodPatcher("Setup", PatchType.Postfix, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)]
        public static void SetupPostfix(Image ___image)
        {
            if (___image != null && (___image.material == null || ___image.material.name == "MapSprite"))
            {
                ___image.material = default;
            }
        }
    }
}
