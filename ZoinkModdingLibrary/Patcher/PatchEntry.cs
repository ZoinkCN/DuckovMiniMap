using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace ZoinkModdingLibrary.Patcher
{
    public class PatchEntry
    {
        private ModLogger modLogger;
        private MethodInfo original;
        public HarmonyMethod? prefix;
        public HarmonyMethod? postfix;
        public HarmonyMethod? transpiler;
        public HarmonyMethod? finalizer;

        public PatchEntry(MethodInfo original, ModLogger? modLogger = null)
        {
            this.original = original;
            this.modLogger = modLogger ?? ModLogger.DefultLogger;
        }

        public bool IsEmpty => prefix == null && postfix == null && transpiler == null && finalizer == null;

        public void Patch(Harmony? harmony)
        {
            if (IsEmpty)
            {
                return;
            }
            try
            {
                harmony?.Unpatch(original, HarmonyPatchType.All, harmony.Id);
                harmony?.Patch(original, prefix, postfix, transpiler, finalizer);
            }
            catch (Exception e)
            {
                modLogger.LogError($"Patch Failed: {e.Message}");
            }
        }

        public void Unpatch(Harmony? harmony)
        {
            harmony?.Unpatch(original, HarmonyPatchType.All, harmony.Id);
        }
    }
}
