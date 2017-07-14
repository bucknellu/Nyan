using Nyan.Core.Shared;

namespace Nyan.Core.Modules.Maintenance
{
    [Priority(Level = -1)]
    public class NullMaintenanceEventHandler : IMaintenanceEventHandler
    {
        public bool CanRun(MaintenanceSchedule maintenanceTask) { return true; }
        public void HandleEvent(IMaintenanceEventEntry pEvent) { }
        public void AfterRun(MaintenanceSchedule maintenanceTask, MaintenanceTaskResult proc) { }
        public bool CanStart() { return true; }
        public void HandleStart() { }

        public void HandleEnd() { }
    }
}