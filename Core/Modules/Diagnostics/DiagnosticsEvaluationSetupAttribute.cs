using System;

namespace Nyan.Core.Modules.Diagnostics {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DiagnosticsEvaluationSetupAttribute : Attribute
    {
        public string Name;
        public string Category;
        public bool Critical;
    }
}