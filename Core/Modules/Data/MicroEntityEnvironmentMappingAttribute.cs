using System;

namespace Nyan.Core.Modules.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class MicroEntityEnvironmentMappingAttribute : Attribute
    {
        public string Origin { get; set; }
        public string Target { get; set; }
    }
}