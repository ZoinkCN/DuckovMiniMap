using MiniMap.Managers;
using MiniMap.Poi;
using MiniMap.Utils;
using ZoinkModdingLibrary.Attributes;
using ZoinkModdingLibrary.Patcher;

namespace MiniMap.Patchers
{
    [TypePatcher(typeof(CharacterSpawnerRoot))]
    public class CharacterSpawnerRootPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new CharacterSpawnerRootPatcher();

        private CharacterSpawnerRootPatcher() { }

        [MethodPatcher("AddCreatedCharacter", PatchType.Prefix)]
        public static bool AddCreatedCharacterPrefix(CharacterMainControl c)
        {
            try
            {
                PoiShows poiShows = new PoiShows()
                {
                    ShowOnlyActivated = ModSettingManager.GetValue("showOnlyActivated", false),
                    ShowPetPoi = ModSettingManager.GetValue("showPetPoi", true),
                    ShowInMap = ModSettingManager.GetValue("showPoiInMap", true),
                    ShowInMiniMap = ModSettingManager.GetValue("showPoiInMiniMap", true),
                };
                PoiCommon.CreatePoiIfNeeded(c, out _, out _, poiShows);
            }
            catch (Exception e)
            {
                ModBehaviour.Logger.LogError($"characterPoi add failed: {e.Message}");
            }
            return true;
        }
    }
}
