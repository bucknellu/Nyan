using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Data
{
    public static class Extensions
    {
        public static void RemoveAll<T>(this List<T> sourceList) where T : MicroEntity<T>
        {
            Parallel.ForEach(sourceList, new ParallelOptions { MaxDegreeOfParallelism = 10 }, i => { i.Remove(); });
        }
    }
}