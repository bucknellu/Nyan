using System;

namespace Nyan.Core.Modules.Data
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class ColumnAttribute : Attribute
    {
        public string Name { get; set; }
    }
}