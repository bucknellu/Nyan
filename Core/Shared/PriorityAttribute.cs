using System;

namespace Nyan.Core.Shared
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PriorityAttribute : Attribute
    {
        public int Level { get; set; }
    }
}