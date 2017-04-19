namespace Nyan.Modules.Web.Tools.Maintenance
{
    public interface IMaintenanceEventHandler
    {
        void HandleEvent(IMaintenanceEventEntry pEvent);
    }
}