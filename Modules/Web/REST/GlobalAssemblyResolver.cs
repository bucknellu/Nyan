using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http.Dispatcher;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Web.REST
{
    public class GlobalAssemblyResolver : IAssembliesResolver
    {
        public ICollection<Assembly> GetAssemblies()
        {
            // Loads all assemblies mapped during resolution time:
            var baseAssemblies = Management.AssemblyCache.Select(i => i.Value).ToList().ToCollection();
            return baseAssemblies;
        }
    }
}