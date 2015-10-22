using System;

namespace Nyan.Core.Modules.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class KeyAttribute : Attribute
    {
    }
}