using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;

namespace Nyan.Modules.Web.REST
{
    public class CustomDirectRouteProvider : DefaultDirectRouteProvider
    {
        public static readonly Dictionary<string , RouteInfo> Routes = new Dictionary<string, RouteInfo>();

        protected override IReadOnlyList<IDirectRouteFactory> GetActionRouteFactories(
            HttpActionDescriptor actionDescriptor)
        {
            var a = new List<RouteAttribute>();

            var ret = actionDescriptor.GetCustomAttributes<IDirectRouteFactory>(true);
            
            foreach (var item in ret)
            {
                string routeprefix = null;

                var attribute = item as RouteAttribute;

                if (attribute == null) continue;

                var subRoute = attribute.Template;

                var method = actionDescriptor.SupportedHttpMethods[0].Method;

                var attribs = actionDescriptor.ControllerDescriptor.ControllerType.CustomAttributes.ToList();

                foreach (var attrib in attribs.Where(attrib => attrib.AttributeType == typeof(RoutePrefixAttribute)))
                    routeprefix = attrib.ConstructorArguments[0].Value.ToString();

                var key = routeprefix + "/" + subRoute;

                if (!Routes.ContainsKey(key))
                {
                    Routes.Add(key, new RouteInfo
                    {
                        Class = actionDescriptor.ControllerDescriptor.ControllerType.FullName,
                        Method = method
                    });
                }
                else
                {
                    Routes[key].Method += " " + method;
                }
            }

            return ret;
        }

        public class RouteInfo
        {
            public string Class;
            public string Method;
        }
    }
}