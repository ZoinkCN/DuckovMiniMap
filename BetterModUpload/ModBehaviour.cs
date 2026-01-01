using System;
using ZoinkModdingLibrary;

namespace BetterModUpload
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static string MOD_NAME = "BetterModUpload";
        private static ModBehaviour? instance;
        public static ModBehaviour? Instance => instance;
        public static ModLogger Logger { get; } = new ModLogger(MOD_NAME);

        private const string MOD_ID = "com.zoink.bettermodupload";


        void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError("ModBehaviour 已实例化");
                return;
            }
            instance = this;
        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {

        }
    }
}
