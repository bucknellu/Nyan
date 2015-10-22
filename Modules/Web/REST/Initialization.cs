using System.Net.Http.Headers;
using System.Web.Http;

namespace Nyan.Modules.Web.REST
{
    public static class Initialization
    {
        public static string UrlPrefix
        {
            get { return "api"; }
        }

        public static string UrlPrefixRelative
        {
            get { return "~/api"; }
        }

        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes(new CustomDirectRouteProvider());
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
        }
    }
}