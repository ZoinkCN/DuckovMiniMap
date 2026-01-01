using System;
using System.Reflection;

namespace ZoinkModdingLibrary.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodPatcherAttribute : Attribute
    {
        private string methodName;
        private BindingFlags bindingFlags;
        private PatchType patchType;

        public string MethodName => methodName;
        public BindingFlags BindingFlags => bindingFlags;
        public PatchType PatchType => patchType;

        public MethodPatcherAttribute(string methodName, PatchType patchType, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            this.methodName = methodName;
            this.bindingFlags = bindingFlags;
            this.patchType = patchType;
        }
    }

    public enum PatchType
    {
        Prefix,
        Postfix,
        Transpiler,
        Finalizer
    }
}
