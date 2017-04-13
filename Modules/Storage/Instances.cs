using System.Linq;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Storage
{
    public static class Instances
    {
        public static IStorageProvider Current { get; internal set; }

        static Instances()
        {
            var p1 = Management.GetClassesByInterface<IStorageProvider>();
            Current = p1.Any() ? p1[0].CreateInstance<IStorageProvider>() : null;
        }
    }
}