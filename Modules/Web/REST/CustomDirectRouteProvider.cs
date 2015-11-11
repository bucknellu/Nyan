using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;

namespace Nyan.Modules.Web.REST
{
    public class CustomDirectRouteProvider : DefaultDirectRouteProvider
    {
        public class RouteInfo
        {
            public string Class;
            public string Route;
            public string Method;

            public override string ToString()
            {
                return Method.PadLeft(6) + " : " + Route;
            }
        }

        public static readonly List<RouteInfo> Routes = new List<RouteInfo>();

        protected override IReadOnlyList<IDirectRouteFactory> GetActionRouteFactories(HttpActionDescriptor actionDescriptor)
        {
            var ret = actionDescriptor.GetCustomAttributes<IDirectRouteFactory>(true);

            List<System.Web.Http.RouteAttribute> a = new List<System.Web.Http.RouteAttribute>();

            foreach (var item in ret)
            {
                string routeprefix = null;

                if (item.GetType() == typeof(System.Web.Http.RouteAttribute))
                {
                    var subRoute = ((System.Web.Http.RouteAttribute)item).Template;

                    var method = actionDescriptor.SupportedHttpMethods[0].Method;

                    var attribs = actionDescriptor.ControllerDescriptor.ControllerType.CustomAttributes.ToList();

                    foreach (var attrib in attribs)
                    {
                        if (attrib.AttributeType == typeof(System.Web.Http.RoutePrefixAttribute))
                        {
                            routeprefix = attrib.ConstructorArguments[0].Value.ToString();
                        }

                    }

                    Routes.Add(new RouteInfo()
                    {
                        Class = actionDescriptor.ControllerDescriptor.ControllerType.FullName,
                        Route = routeprefix + "/" + subRoute,
                        Method = method
                    });
                }
            }

            return ret;
        }
    }
}