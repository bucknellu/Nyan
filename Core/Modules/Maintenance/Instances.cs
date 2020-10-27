using System;
using System.Collections.Generic;
using System.Linq;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Shared;

namespace Nyan.Core.Modules.Maintenance
{
    public static class Instances
    {
        public static readonly List<MaintenanceSchedule> Schedule;

        public static Dictionary<int, List<MaintenanceSchedule>> GetScheduledTasksByPriority() { return GetScheduledTasksByPriority(false); }

        public static Dictionary<int, List<MaintenanceSchedule>> GetSubTasksByPriority(this Type parentMaintenanceType, bool onlyLocal = false)
        {
            return GetScheduledTasksByPriority(onlyLocal, parentMaintenanceType);
        }

        public static Dictionary<int, List<MaintenanceSchedule>> GetScheduledTasksByPriority(bool onlyLocal, Type parentMaintenanceType = null)
        {
            var ret = new Dictionary<int, List<MaintenanceSchedule>>();
            List<MaintenanceSchedule> src = null;

            src = onlyLocal ? Schedule.Where(i => i.Source == Configuration.ApplicationAssemblyName).ToList() : Schedule;

            if (parentMaintenanceType != null)
            {
                var parentName = parentMaintenanceType.FullName;
                src = src.Where(i => i.ParentTaskFullName == parentName).ToList();
            }
            else
            {
                src = src.Where(i => i.ParentTaskFullName == null).ToList();
            }

            foreach (var ms in src)
            {
                if (!ret.ContainsKey(ms.Priority)) ret.Add(ms.Priority, new List<MaintenanceSchedule>());
                ret[ms.Priority].Add(ms);
            }

            return ret;
        }

        private static IMaintenanceEventHandler _handler;

        static Instances()
        {
            var ret = new List<MaintenanceSchedule>();

            foreach (var i in RegisteredMaintenanceTaskTypes)
            {
                var setup = (MaintenanceTaskSetupAttribute)i.GetMethod("MaintenanceTask").GetCustomAttributes(typeof(MaintenanceTaskSetupAttribute), false).FirstOrDefault()
                            ?? new MaintenanceTaskSetupAttribute { Name = "Maintenance Task" };

                var priority = (PriorityAttribute)i.GetMethod("MaintenanceTask")
                                   .GetCustomAttributes(typeof(PriorityAttribute), false)
                                   .FirstOrDefault()
                               ?? new PriorityAttribute { Level = 0 };

                var entry = new MaintenanceSchedule
                {
                    Setup = setup,
                    Priority = priority.Level,
                    Task = i.CreateInstance<IMaintenanceTask>(),
                    Id = (i.FullName + ": " + setup.Name).MetaHash(),
                    Namespace = i.FullName,
                    Name = setup.Name,
                    Schedule = setup.ScheduleTimeSpan,
                    Source = i.Assembly.GetName().Name,
                    ParentTaskFullName =setup.ParentTask?.FullName
                };

                ret.Add(entry);
            }

            ret = ret.OrderBy(i => i.Priority * -1).ToList();

            Schedule = ret;
        }

        public static MaintenanceSchedule GetMaintenanceSchedule(MaintenanceSchedule target)
        {
            return Schedule.FirstOrDefault(i => i == target);
        }

        internal static IMaintenanceEventHandler Handler
        {
            get
            {
                if (_handler != null) return _handler;

                _handler = MaintenanceEventHandlerType.CreateInstance<IMaintenanceEventHandler>();
                return _handler;
            }
        }

        internal static Type MaintenanceEventHandlerType => Management.GetClassesByInterface<IMaintenanceEventHandler>()[0];

        internal static IEnumerable<Type> RegisteredMaintenanceTaskTypes => Management.GetClassesByInterface<IMaintenanceTask>();
    }
}