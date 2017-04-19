using System;
using System.Collections.Generic;

namespace Nyan.Modules.Web.Tools.Maintenance
{
    public class MaintenanceEventEntry : IMaintenanceEventEntry
    {
        public int TaskCount { get; set; }
        public List<MaintenanceTaskResult> Results { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public TimeSpan ElapsedTime { get; set; }
    }
}