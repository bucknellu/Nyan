using System;
using System.Collections.Generic;
using System.Linq;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;

namespace Nyan.Core.Modules.Diagnostics
{
    public static class Instances
    {
        internal static readonly IEnumerable<Type> RegisteredDiagnosticsTypes = Management.GetClassesByInterface<IDiagnosticsEvaluation>();

        public static readonly Dictionary<DiagnosticsEvaluationSetupAttribute, IDiagnosticsEvaluation> Evaluators;

        static Instances()
        {
            Evaluators = RegisteredDiagnosticsTypes.ToDictionary(
                i => (DiagnosticsEvaluationSetupAttribute) i.GetMethod("RunDiagnostics")
                         ?.GetCustomAttributes(typeof(DiagnosticsEvaluationSetupAttribute), false)
                         .FirstOrDefault()
                     ?? new DiagnosticsEvaluationSetupAttribute {Name = "Diagnostics Task"},
                i => i.CreateInstance<IDiagnosticsEvaluation>());

        }
    }
}