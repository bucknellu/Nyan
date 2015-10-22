using System;

namespace Nyan.Core.Settings
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PackagePriorityAttribute : Attribute
    {
        public int Level { get; set; }
    }
}