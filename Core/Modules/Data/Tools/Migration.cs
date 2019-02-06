using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data.Contracts;

namespace Nyan.Core.Modules.Data.Tools
{
    public static class Migration
    {
        public static void RunAll()
        {
            var allTypes = Management.GetClassesByInterface<IMigration>().ToInstances<IMigration>();

            foreach (var migration in allTypes) migration.MigrateStart();
        }
    }
}