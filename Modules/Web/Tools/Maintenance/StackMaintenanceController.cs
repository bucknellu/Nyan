using System.Web.Http;
using Nyan.Core.Modules.Maintenance;

namespace Nyan.Modules.Web.Tools.Maintenance
{
    [RoutePrefix("stack/tools/maintenance")]
    public class StackMaintenanceController : ApiController
    {
        [Route("run")]
        [HttpGet]
        public IMaintenanceEventEntry DoMaintenance()
        {
            return Factory.DoMaintenance();
        }

        [Route("task/list")]
        [HttpGet]
        public object ListMaintenanceTasks()
        {
            return Instances.Schedule;
        }
    }
}