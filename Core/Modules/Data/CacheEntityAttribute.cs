using System;

namespace Nyan.Core.Modules.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CacheEntityAttribute : Attribute
    {
        // ReSharper restore InconsistentNaming
        [NotNull] public string IdentifierPropertyName;
    }
}