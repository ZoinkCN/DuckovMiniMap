//using HarmonyLib;
//using System.Reflection;
//using UnityEngine;

//namespace MiniMap.Patchers
//{
//    public class PatchEntry
//    {
//        private MethodInfo original;
//        public HarmonyMethod? prefix;
//        public HarmonyMethod? postfix;
//        public HarmonyMethod? transpiler;
//        public HarmonyMethod? finalizer;

//        public PatchEntry(MethodInfo original)
//        {
//            this.original = original;
//        }

//        public bool IsEmpty => prefix == null && postfix == null && transpiler == null && finalizer == null;

//        public void Patch()
//        {
//            if (IsEmpty)
//            {
//                return;
//            }
//            try
//            {
//                ModBehaviour.Instance?.Harmony.Unpatch(original, HarmonyPatchType.All, ModBehaviour.Instance.Harmony.Id);
//                ModBehaviour.Instance?.Harmony.Patch(original, prefix, postfix, transpiler, finalizer);
//            }
//            catch (Exception e)
//            {
//                ModLogger.DefultLogger.LogError($"Patch Failed: {e.Message}");
//            }
//        }

//        public void Unpatch()
//        {
//            ModBehaviour.Instance?.Harmony.Unpatch(original, HarmonyPatchType.All, ModBehaviour.Instance.Harmony.Id);
//        }
//    }
//}
