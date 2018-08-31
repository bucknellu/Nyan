using System;

namespace Nyan.Core.Modules.Maintenance {
    public class MaintenanceSchedule
    {
        public string Id;
        public int Priority;
        public DateTime LastRun;
        public TimeSpan Schedule;
        public MaintenanceTaskSetupAttribute Setup;
        internal IMaintenanceTask Task;
        public string Namespace { get; set; }
        public string Name { get; set; }
    }
}