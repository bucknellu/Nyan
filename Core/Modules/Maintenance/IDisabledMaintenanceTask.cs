namespace Nyan.Core.Modules.Maintenance {
    public interface IDisabledMaintenanceTask
    {
        MaintenanceTaskResult MaintenanceTask(bool force);
    }
}