using System.Collections.Generic;
using System.Linq;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data.Contracts;
using Nyan.Core.Shared;

namespace Nyan.Core.Modules.Data.Tools
{
    public static class Migration
    {
        private static Dictionary<string, IMigration> _instances;
        public static object _lock = new object();

        public static Dictionary<string, IMigration> Instances
        {
            get
            {
                lock (_lock)
                {

                    if (_instances != null) return _instances;

                    var pre = Management.GetClassesByInterface<IMigration>()
                        .OrderBy(i => ((PriorityAttribute)i.GetMethod("MigrationTask")?.GetCustomAttributes(typeof(PriorityAttribute), false).FirstOrDefault() ?? new PriorityAttribute { Level = 0 }).Level)
                        .ToInstances<IMigration>().ToList();

                    _instances = pre.ToDictionary(i => i.GetType().FullName.Sha512Hash(), i => i);

                    return _instances;
                }

            }
        }

        public static void RunAll()
        {

            foreach (var migration in Instances.Values) migration.MigrationTask();
        }
    }
}