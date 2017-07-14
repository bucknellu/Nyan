using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Maintenance
{
    public static class Tools
    {
        public static IMaintenanceEventEntry DoMaintenance()
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
                    Current.Log.Add("DoMaintenance: START " + maintenanceTask.GetType().FullName, Message.EContentType.Maintenance);

                    var sw = new Stopwatch();
                    sw.Start();

                    var proc = new MaintenanceTaskResult();

                    try
                    {
                        proc = maintenanceTask.MaintenanceTask();

                        if (proc.Status == MaintenanceTaskResult.EResultStatus.Undefined) proc.Status = MaintenanceTaskResult.EResultStatus.Success;

                        if (proc.Message == null) proc.Message = "SUCCESS";
                    } catch (Exception e)
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

                try { Instances.Handler.HandleEvent(mRet); } catch { }

                return mRet;
            } catch (Exception e)
            {
                if (currType == "") Current.Log.Add(e);
                else Current.Log.Add(e, currType);
                throw;
            }
        }
    }
}