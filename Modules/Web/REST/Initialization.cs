using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Modules.Web.REST.auth;

namespace Nyan.Modules.Web.REST
{
    public static class Initialization
    {
        public static string UrlPrefix { get { return "api"; } }

        public static string UrlPrefixRelative { get { return "~/api"; } }

        public static void Register(HttpConfiguration config)
        {
            // Force load of all Controllers:
            if (Current.WebApiCORSDomains != null)
            {
                var corsAttr = new EnableCorsAttribute(Current.WebApiCORSDomains, "*", "*");
                corsAttr.SupportsCredentials = true;
                config.EnableCors(corsAttr);
            }

            config.SuppressHostPrincipal(); //Isolates WebApi Auth form Host (IIS) Auth

            config.Filters.Add(new NyanAuthenticationFilter());

            config.MapHttpAttributeRoutes(new CustomDirectRouteProvider());
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));

            Current.Log.Add("WebApi REST       : Routes registered.", Message.EContentType.Info);
        }
    }
}