using System;
using System.Collections.Generic;

namespace Nyan.Modules.Web.Tools.Maintenance
{
    public interface IMaintenanceEventEntry
    {
        TimeSpan ElapsedTime { get; set; }
        List<MaintenanceTaskResult> Results { get; set; }
        int TaskCount { get; set; }
        DateTime Timestamp { get; set; }
    }
}