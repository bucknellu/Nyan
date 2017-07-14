namespace Nyan.Core.Modules.Maintenance
{
    public interface IMaintenanceEventHandler
    {
        bool CanRun(MaintenanceSchedule maintenanceTask);
        void HandleEvent(IMaintenanceEventEntry pEvent);
        void AfterRun(MaintenanceSchedule maintenanceTask, MaintenanceTaskResult proc);
        bool CanStart();
        void HandleStart();
        void HandleEnd();
    }
}