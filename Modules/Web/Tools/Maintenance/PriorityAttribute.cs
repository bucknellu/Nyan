using System;

namespace Nyan.Modules.Web.Tools.Maintenance
{
    public class MaintenanceTaskResult
    {
        public enum EResultStatus
        {
            Success,
            Undefined,
            Failed,
            Warning
        }

        public EResultStatus Status { get; set; } = EResultStatus.Undefined;
        public string TaskScheduler { get; set; }
        public string Message { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    }
    public interface IMaintenanceTask
    {
        MaintenanceTaskResult MaintenanceTask();
    }
}