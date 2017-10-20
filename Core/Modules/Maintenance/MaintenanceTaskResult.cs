using System;

namespace Nyan.Core.Modules.Maintenance {
    public class MaintenanceTaskResult
    {
        public enum EResultStatus
        {
            Success,
            Undefined,
            Failed,
            Warning,
            Skipped
        }

        public EResultStatus Status { get; set; } = EResultStatus.Undefined;
        public string Message { get; set; }
        public TagClicker Counters { get; set; }
        public string Id { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public int Priority { get; set; }
    }
}