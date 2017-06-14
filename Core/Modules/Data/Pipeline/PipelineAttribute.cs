using System;

namespace Nyan.Core.Modules.Data.Pipeline
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PipelineAttribute : Attribute
    {
        [NotNull]
        public Type[] Types;
        public PipelineAttribute(params Type[] types) { Types = types; }
    }
}