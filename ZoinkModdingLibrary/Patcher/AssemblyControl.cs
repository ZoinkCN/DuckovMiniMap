using System;
using System.Reflection;
using UnityEngine;

namespace ZoinkModdingLibrary.Patcher
{
    public static class AssemblyOption
    {
        public static Type? FindTypeInAssemblies(string assembliyName, string typeName, ModLogger? logger = null)
        {
            logger ??= ModLogger.DefultLogger;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.FullName.Contains(assembliyName))
                {
                    logger.Log($"找到{assembliyName}相关程序集: {assembly.FullName}");
                }

                Type type = assembly.GetType(typeName);
                if (type != null) return type;
            }

            logger.LogError($"找不到程序集{assembliyName}");
            return null;
        }
    }
}
