using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.ExceptionHandling;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Modules.Web.REST.auth;
using Nyan.Modules.Web.REST.CORS;
using Nyan.Modules.Web.REST.formatters;
using Nyan.Modules.Web.REST.formatters.jsonp;
using Nyan.Modules.Web.REST.RSS;

namespace Nyan.Modules.Web.REST
{
    public static class Initialization
    {
        public static string UrlPrefix => "api";

        public static string UrlPrefixRelative => "~/api";

        public static void Register(HttpConfiguration config)
        {
            // Force load of all Controllers:
            if (Current.WebApiCORSDomains != null)
                if (Current.WebApiCORSDomainMasks != null)
                {
                    Current.Log.Add($"WebApi REST       : {Current.WebApiCORSDomainMasks.Count} CORS domain masks.", Message.EContentType.StartupSequence);
                    config.AddCustomCorsFactory();
                }
                else
                {
                    var items = Current.WebApiCORSDomains.Split(',');
                    var corsAttr = new EnableCorsAttribute(Current.WebApiCORSDomains, "*", "*") {SupportsCredentials = true};
                    Current.Log.Add($"WebApi REST       : {items.Length} CORS domains allowed.", Message.EContentType.StartupSequence);
                    config.EnableCors(corsAttr);
                }

            config.Services.Add(typeof(IExceptionLogger), new GlobalErrorHandler());

            config.SuppressHostPrincipal(); //Isolates WebApi Auth form Host (IIS) Auth

            config.Filters.Add(new NyanAuthenticationFilter());
            config.Filters.Add(new HandleApiExceptionAttribute());

            config.MapHttpAttributeRoutes(new CustomDirectRouteProvider());

            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.Add(new XmlMediaTypeFormatter());
            config.Formatters.Add(new CsvMediaTypeFormatter());
            config.Formatters.Add(new SyndicationFeedFormatter());
            config.AddJsonpFormatter();

            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            //config.Formatters.Add(new CsvMediaTypeFormatter());

            Current.Log.Add("WebApi REST       : Routes registered.", Message.EContentType.Info);
        }
    }
}