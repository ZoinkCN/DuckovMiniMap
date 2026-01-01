using MiniMap.Utils;
using System.Reflection;
using ZoinkModdingLibrary.Attributes;
using ZoinkModdingLibrary.Patcher;

namespace MiniMap.Patchers
{
    [TypePatcher(typeof(CharacterMainControl))]
    public class CharacterMainControlPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new CharacterMainControlPatcher();
        private CharacterMainControlPatcher() { }

        [MethodPatcher("Update", PatchType.Postfix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void UpdatePostfix(CharacterMainControl __instance)
        {
            try
            {
                PoiCommon.CreatePoiIfNeeded(__instance, out _, out _);
            }
            catch (Exception e)
            {
                ModBehaviour.Logger.LogError($"characterPoi update failed: {e.Message}");
            }
        }
    }
}
