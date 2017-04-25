using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;
using Nyan.Core.Extensions;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.Tools.Maintenance
{
    [RoutePrefix("stack/tools/maintenance")]
    public class StackMaintenanceController : ApiController
    {
        [Route("run")]
        [HttpGet]
        public IMaintenanceEventEntry DoMaintenance()
        {
            var ret = new List<MaintenanceTaskResult>();
            var msw = new Stopwatch();

            var currType = "";

            msw.Start();

            try
            {
                var instances = Instances.RegisteredMaintenanceTaskTypes.Select(i =>
                {
                    currType = i.FullName;
                    return i.CreateInstance<IMaintenanceTask>();
                }).ToList();

                foreach (var maintenanceTask in instances)
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    var proc = new MaintenanceTaskResult();

                    try
                    {
                        proc = maintenanceTask.MaintenanceTask();

                        if (proc.Status == MaintenanceTaskResult.EResultStatus.Undefined) proc.Status = MaintenanceTaskResult.EResultStatus.Success;

                        if (proc.Message == null) proc.Message = "SUCCESS";
                    }
                    catch (Exception e)
                    {
                        proc.Status = MaintenanceTaskResult.EResultStatus.Failed;
                        proc.Message = maintenanceTask.GetType().FullName + ": ERROR " + e.Message;
                    }

                    sw.Stop();

                    proc.Duration = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
                    proc.TaskScheduler = maintenanceTask.GetType().FullName;

                    ret.Add(proc);
                }

                msw.Stop();

                var mRet = new MaintenanceEventEntry
                {
                    TaskCount = instances.Count,
                    Results = ret,
                    ElapsedTime = TimeSpan.FromMilliseconds(msw.ElapsedMilliseconds)
                };

                try { Instances.Handler.HandleEvent(mRet); }
                catch { }


                return mRet;
            }
            catch (Exception e)
            {
                if (currType == "") Current.Log.Add(e);
                else Current.Log.Add(e, currType);
                throw;
            }
        }
    }
}