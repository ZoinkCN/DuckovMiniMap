using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Text;
using ZoinkModdingLibrary.Patcher;

namespace ZoinkModdingLibrary.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TypePatcherAttribute : Attribute
    {
        private string targetAssemblyName;
        private string targetTypeName;
        private Type? targetType;

        public string TargetAssemblyName => targetAssemblyName;
        public string TargetTypeName => targetTypeName;
        public Type? TargetType => targetType;

        public TypePatcherAttribute(string targetAssemblyName, string targetTypeName)
        {
            this.targetAssemblyName = targetAssemblyName;
            this.targetTypeName = targetTypeName;
            targetType = AssemblyOption.FindTypeInAssemblies(targetAssemblyName, targetTypeName);
        }

        public TypePatcherAttribute(Type targetType)
        {
            this.targetType = targetType;
            this.targetAssemblyName = targetType.Assembly.FullName;
            this.targetTypeName = targetType.Name;
        }
    }
}
