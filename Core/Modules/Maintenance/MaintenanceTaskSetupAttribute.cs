using System;

namespace Nyan.Core.Modules.Maintenance {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MaintenanceTaskSetupAttribute : Attribute
    {
        public string Name;
        public TimeSpan ScheduleTimeSpan = TimeSpan.FromMinutes(30);  // Default behavior: run every 30 mins.
        public bool RunOnce = false;
        public string Schedule
        {
            get { return ScheduleTimeSpan.ToString(); }
            set { ScheduleTimeSpan = TimeSpan.Parse(value); }
        }
    }
}