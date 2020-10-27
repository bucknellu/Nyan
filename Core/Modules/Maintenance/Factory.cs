using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Maintenance
{
    public static class Factory
    {

        public static void ProcessTask(this List<MaintenanceTaskResult> resultSet, MaintenanceSchedule maintenanceTask, bool force)
        {
            if (resultSet == null) resultSet = new List<MaintenanceTaskResult>();

            var proc = new MaintenanceTaskResult { Id = maintenanceTask.Id };
            var mustSkip = false;

            if (!force)
                if (!Instances.Handler.CanRun(maintenanceTask))
                {
                    proc.Status = MaintenanceTaskResult.EResultStatus.Skipped;
                    proc.Message = "Skipped";
                    mustSkip = true;
                }

            if (!mustSkip)
            {
                Current.Log.Add("START " + maintenanceTask.Name, Message.EContentType.Maintenance);

                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    proc = maintenanceTask.Task.MaintenanceTask(force);
                    if (proc.Status == MaintenanceTaskResult.EResultStatus.Undefined) proc.Status = MaintenanceTaskResult.EResultStatus.Success;
                    if (proc.Message == null) proc.Message = "SUCCESS";

                    // Are there sub-tasks?
                    var subTasks = Instances.GetSubTasksByPriority(maintenanceTask.Task.GetType(), false).ToList();

                    if (subTasks.Count > 0)
                    {

                        Current.Log.Add($"    START: {subTasks.Count} subtasks", Message.EContentType.Maintenance);

                        foreach (var st in subTasks)
                        {
                            foreach (var stPerPrio in st.Value)
                            {
                                resultSet.ProcessTask(stPerPrio, force);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    proc.Status = MaintenanceTaskResult.EResultStatus.Failed;
                    proc.Message = $"ERROR: {e.Message} @ {e.FancyString()}";
                }

                proc.Id = maintenanceTask.Id;
                proc.Priority = maintenanceTask.Priority;

                sw.Stop();

                proc.Duration = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
            }

            resultSet.Add(proc);

            Instances.Handler.AfterRun(maintenanceTask, proc);


        }

        public static IMaintenanceEventEntry DoMaintenance(bool force = false, bool saveLog = true) { return DoMaintenance(force, saveLog, false); }

        public static IMaintenanceEventEntry DoMaintenance(bool force, bool saveLog, bool onlyLocal = false)
        {
            var ret = new List<MaintenanceTaskResult>();
            var msw = new Stopwatch();

            var currType = "";

            if (!Instances.Handler.CanStart()) { }
            //throw new Exception("Cannot start maintenance.");}

            Instances.Handler.HandleStart();

            msw.Start();

            Current.Log.Add($"Starting Maintenance: {Instances.Schedule} tasks, force:{force}, saveLog: {saveLog}", Message.EContentType.Maintenance);

            try
            {
                var scheduleByPriority = Instances.GetScheduledTasksByPriority(onlyLocal);

                Current.Log.Add("    Warm-up summary:", Message.EContentType.Maintenance);

                foreach (var scheduleInstance in scheduleByPriority) Current.Log.Add($"        Priority {scheduleInstance.Key}: {scheduleInstance.Value.Count} Task(s)", Message.EContentType.Maintenance);

                foreach (var scheduleInstance in scheduleByPriority)
                {
                    Current.Log.Add($"    START: Priority {scheduleInstance.Key} tasks ", Message.EContentType.Maintenance);

                    try
                    {
                        Parallel.ForEach(scheduleInstance.Value, maintenanceTask =>
                        {
                            try
                            {

                                ret.ProcessTask(maintenanceTask, force);

                            }
                            catch (Exception e)
                            {
                                Current.Log.Add($"    TASK [{maintenanceTask.Name}] FAILURE: {e.Message} @ {e.FancyString()}", Message.EContentType.Maintenance);
                                Current.Log.Add(e);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Current.Log.Add($"    FAILURE: {e.Message} @ {e.FancyString()}", Message.EContentType.Maintenance);
                        Current.Log.Add(e);
                    }
                }

                msw.Stop();

                // Save anyway if there's a Task error.
                foreach (var maintenanceTaskResult in ret) if (maintenanceTaskResult.Status == MaintenanceTaskResult.EResultStatus.Failed) saveLog = true;

                var mRet = new MaintenanceEventEntry
                {
                    TaskCount = Instances.Schedule.Count,
                    ElapsedTime = TimeSpan.FromMilliseconds(msw.ElapsedMilliseconds),
                    Process = Configuration.ApplicationAssemblyName,
                    Path = Configuration.BaseDirectory
                };

                foreach (var mtr in ret)
                {
                    if (mtr.Status == MaintenanceTaskResult.EResultStatus.Skipped) continue;
                    if (!mRet.Results.ContainsKey(mtr.Priority)) mRet.Results.Add(mtr.Priority, new List<MaintenanceTaskResult>());
                    mRet.Results[mtr.Priority].Add(mtr);
                }

                if (!saveLog) return mRet;
                try { Instances.Handler.HandleEvent(mRet); } catch { }

                Instances.Handler.HandleEnd();

                return mRet;
            }
            catch (Exception e)
            {
                if (currType == "") Current.Log.Add(e);
                else Current.Log.Add(e, currType);

                Instances.Handler.HandleEnd();

                throw;
            }
        }
    }
}