using HarmonyLib;

namespace ZoinkModdingLibrary.Patcher
{
    public interface IPatcher
    {
        public bool IsPatched { get; }

        public abstract bool Patch(Harmony? harmony, ModLogger? logger);

        public abstract void Unpatch(Harmony? harmony, ModLogger? logger);
    }
}
