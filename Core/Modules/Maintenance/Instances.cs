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

        public static Dictionary<int, List<MaintenanceSchedule>> GetScheduledTasksByPriority(bool onlyLocal)
        {
            var ret = new Dictionary<int, List<MaintenanceSchedule>>();

            var src = onlyLocal ? Schedule.Where(i => i.Source == Nyan.Core.Configuration.ApplicationAssemblyName).ToList() : Schedule;


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
                    Source = i.Assembly.GetName().Name
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