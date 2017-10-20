using System;
using System.Collections.Generic;

namespace Nyan.Core.Modules.Maintenance
{
    public interface IMaintenanceEventEntry
    {
        string Process { get; set; }
        string Path { get; set; }
        TimeSpan ElapsedTime { get; set; }
        Dictionary<int, List<MaintenanceTaskResult>> Results { get; set; }
        int TaskCount { get; set; }
        DateTime Timestamp { get; set; }
    }
}