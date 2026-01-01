using System;
using UnityEngine;

namespace ZoinkModdingLibrary
{
    public class ModLogger
    {
        public static ModLogger DefultLogger { get; } = new ModLogger("ZoinkModdingLibrary");

        private string modName;

        public ModLogger(string modName)
        {
            this.modName = modName;
        }

        public void Log(string message)
        {
            Debug.Log($"[{modName}] {message}");
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning($"[{modName}] {message}");
        }

        public void LogError(string message)
        {
            Debug.LogError($"[{modName}] {message}");
        }

        public void LogException(Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
