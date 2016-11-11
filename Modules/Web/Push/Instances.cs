using System.Linq;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Modules.Web.Push.Primitives;

namespace Nyan.Modules.Web.Push
{
    public static class Instances
    {
        public static IAuthPrimitive Auth;
        public static DispatcherPrimitive Dispatcher;

        static Instances()
        {
            var p1 = Management.GetClassesByInterface<IAuthPrimitive>();
            Auth = p1.Any() ? p1[0].CreateInstance<IAuthPrimitive>() : null;

            var p2 = Management.GetClassesByInterface<DispatcherPrimitive>();
            Dispatcher = p2.Any() ? p2[0].CreateInstance<DispatcherPrimitive>() : new DispatcherPrimitive();
        }
    }
}