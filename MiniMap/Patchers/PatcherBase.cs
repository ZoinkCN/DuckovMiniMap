//using HarmonyLib;
//using MiniMap.Attributes;
//using MiniMap.Utils;
//using Sirenix.Utilities;
//using System.Reflection;
//using Unity.VisualScripting;
//using UnityEngine;

//namespace MiniMap.Patchers
//{
//    public abstract class PatcherBase : IPatcher
//    {
//        public static PatcherBase? Instance { get; }

//        private Type? targetType = null;
//        private bool isPatched = false;

//        public virtual bool IsPatched => isPatched;

//        public virtual bool Patch()
//        {
//            try
//            {
//                if (isPatched)
//                {
//                    return true;
//                }
//                TypePatcherAttribute typePatcher = GetType().GetCustomAttribute<TypePatcherAttribute>();
//                if (typePatcher == null)
//                {
//                    Debug.LogError($"{GetType().Name} needs \"{typeof(TypePatcherAttribute).Name}\" Attribute");
//                    return false;
//                }
//                targetType = typePatcher.TargetType;
//                if (targetType == null)
//                {
//                    ModLogger.DefultLogger.LogWarning($"Target Assembly \"{typePatcher.TargetAssemblyName}\" Or Type \"{typePatcher.TargetTypeName}\" Not Found!");
//                    return false;
//                }
//                ModLogger.DefultLogger.Log($"Patching {targetType.Name}");
//                IEnumerable<MethodInfo> patchMethods = GetType().GetMethods().Where(s => s.HasAttribute<MethodPatcherAttribute>());
//                Dictionary<string, PatchEntry> queue = new Dictionary<string, PatchEntry>();
//                ModLogger.DefultLogger.Log($"Find {patchMethods.Count()} Methods to patch");
//                foreach (MethodInfo method in patchMethods)
//                {
//                    MethodPatcherAttribute? methodPatcher = method.GetCustomAttribute<MethodPatcherAttribute>();
//                    if (methodPatcher == null)
//                    {
//                        continue;
//                    }
//                    string targetMethod = methodPatcher.MethodName;
//                    PatchType patchType = methodPatcher.PatchType;
//                    MethodInfo? originalMethod =  targetType.GetMethod(targetMethod, methodPatcher.BindingFlags);
//                    if (originalMethod == null)
//                    {
//                        ModLogger.DefultLogger.LogWarning($"Target Method \"{targetType.Name}.{targetMethod}\" Not Found!");
//                        continue;
//                    }
//                    ModLogger.DefultLogger.Log($"Patching {targetType.Name}.{originalMethod.Name}");
//                    PatchEntry entry;
//                    if (queue.ContainsKey(originalMethod.ToString()))
//                    {
//                        entry = queue[originalMethod.ToString()];
//                    }
//                    else
//                    {
//                        entry = new PatchEntry(originalMethod);
//                        queue.Add(originalMethod.ToString(), entry);
//                    }
//                    switch (patchType)
//                    {
//                        case PatchType.Prefix:
//                            entry.prefix = new HarmonyMethod(method);
//                            break;
//                        case PatchType.Postfix:
//                            entry.postfix = new HarmonyMethod(method);
//                            break;
//                        case PatchType.Transpiler:
//                            entry.transpiler = new HarmonyMethod(method);
//                            break;
//                        case PatchType.Finalizer:
//                            entry.finalizer = new HarmonyMethod(method);
//                            break;
//                        default:
//                            ModLogger.DefultLogger.LogWarning($"Unknown Patch Type \"{patchType}\".");
//                            break;
//                    }
//                }
//                foreach (KeyValuePair<string, PatchEntry> item in queue)
//                {
//                    item.Value.Patch();
//                    ModLogger.DefultLogger.Log($"{item.Key} Patched");
//                }
//                isPatched = true;
//                return true;
//            }
//            catch (Exception e)
//            {
//                ModLogger.DefultLogger.LogError($"Error When Patching: {e.Message}");
//                return false;
//            }
//        }

//        public virtual void Unpatch()
//        {
//            if (isPatched)
//            {

//            }
//        }
//    }
//}
