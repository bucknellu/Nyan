using System;
using System.Collections.Generic;

namespace Nyan.Core.Modules.Maintenance
{
    public class MaintenanceEventEntry : IMaintenanceEventEntry
    {
        public Dictionary<int, List<MaintenanceTaskResult>> Results { get; set; } = new Dictionary<int, List<MaintenanceTaskResult>>();
        public int TaskCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Process { get; set; }
        public string Path { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}