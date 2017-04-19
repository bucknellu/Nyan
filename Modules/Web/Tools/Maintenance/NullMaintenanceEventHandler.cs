using Nyan.Core.Shared;

namespace Nyan.Modules.Web.Tools.Maintenance
{
    [Priority(Level = -1)]
    public class NullMaintenanceEventHandler : IMaintenanceEventHandler
    {
        public void HandleEvent(IMaintenanceEventEntry pEvent) { }
    }
}