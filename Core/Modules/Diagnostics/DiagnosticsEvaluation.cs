using System.Collections.Generic;

namespace Nyan.Core.Modules.Diagnostics
{
    public class DiagnosticsEvaluation
    {
        public enum EState
        {
            Ok,
            Warning,
            Critical,
            Unknown
        }

        public EState State = EState.Unknown;
        public string Message;

        public Dictionary<string, EState> Subtopics = new Dictionary<string, EState>();
    }
}