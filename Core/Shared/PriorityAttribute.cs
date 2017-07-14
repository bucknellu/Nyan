using System;

namespace Nyan.Core.Shared
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class PriorityAttribute : Attribute
    {
        public int Level { get; set; }
    }
}