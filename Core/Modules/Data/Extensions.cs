using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nyan.Core.Modules.Data.Contracts;

namespace Nyan.Core.Modules.Data
{
    public static class Extensions
    {

        public static EntityReference ToReference<T>(this MicroEntity<T> source) where T : MicroEntity<T> => new EntityReference { Id = source.GetEntityIdentifier(), Label = source.GetEntityLabel() };

        public static T ToEntity<T>(this EntityReference source) where T : MicroEntity<T> => MicroEntity<T>.Get(source.Id);

        public static List<T> ToEntityList<T>(this IEnumerable<EntityReference> source) where T : MicroEntity<T>
        {
            var entityReferences = source.ToList();
            return MicroEntity<T>.Get(entityReferences.Select(i => i.Id).Where(i => i != null)).ToList();
        }

        public static void RemoveAll<T>(this List<T> sourceList) where T : MicroEntity<T>
        {
            Parallel.ForEach(sourceList, new ParallelOptions { MaxDegreeOfParallelism = 10 }, i => { i.Remove(); });
        }
    }
}