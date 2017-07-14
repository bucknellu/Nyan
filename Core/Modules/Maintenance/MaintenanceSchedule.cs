using System;

namespace Nyan.Core.Modules.Maintenance {
    public class MaintenanceSchedule
    {
        public string Id;
        public int Priority;
        public DateTime LastRun;
        public TimeSpan Schedule;
        internal MaintenanceTaskSetupAttribute Setup;
        internal IMaintenanceTask Task;
    }
}