namespace Nyan.Core.Modules.Maintenance
{
    public interface IMaintenanceTask
    {
        MaintenanceTaskResult MaintenanceTask(bool force);
    }
    public interface IDisabledMaintenanceTask
    {
        MaintenanceTaskResult MaintenanceTask(bool force);
    }
}