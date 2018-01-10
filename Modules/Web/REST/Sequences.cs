using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Nyan.Core.Assembly;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST
{
    /// <summary>
    ///     Initialization hooks for the REST module.
    /// </summary>
    public static class Sequences
    {
        /// <summary>
        ///     Start-up sequence hook for the rest module. Register and initializes WebApi routes.
        /// </summary>
        public static void Start()
        {
            var apictrls = Management.GetClassesByBaseClass(typeof(ApiController)).ToList();
            Core.Modules.Log.System.Add(apictrls.Count + " WebApi Controller classes loaded");

            // Replaces the default resolver: Forces load of ApiControllers even if they're in a remote assembly
            GlobalConfiguration.Configuration.Services.Replace(typeof(IAssembliesResolver), new GlobalAssemblyResolver());

            GlobalConfiguration.Configure(Initialization.Register);

            Core.Modules.Log.System.Add(CustomDirectRouteProvider.Routes.Count + " WebApi endpoints generated");

            //foreach (var item in CustomDirectRouteProvider.Routes)
            //{
            //    Core.Modules.Log.System.Add(item.Value.Method.PadLeft(17) + " : " + item.Key, Message.EContentType.MoreInfo);
            //}
        }

        /// <summary>
        ///     Shutdown sequence hook for the rest module. Intercepts and logs the reason, and call the core shutdown sequence.
        /// </summary>
        public static void End()
        {
            var runtime = (HttpRuntime) typeof(HttpRuntime).InvokeMember("_theRuntime",
                                                                         BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField,
                                                                         null, null, null);

            if (runtime != null)
            {
                var shutDownMessage = (string) runtime.GetType().InvokeMember("_shutDownMessage",
                                                                              BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
                                                                              null, runtime, null);

                var shutDownStack = (string) runtime.GetType().InvokeMember("_shutDownStack",
                                                                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
                                                                            null, runtime, null);

                var msg = shutDownMessage.Replace(System.Environment.NewLine, ":").Split(':')[0];

                Core.Process.Sequences.End(msg);
            }
            else { Core.Process.Sequences.End(); }
        }

        public static void HandleError(HttpRequest request, HttpResponse response, HttpContext context, HttpServerUtility server)
        {
            // Code that runs when an unhandled error occurs

            // Get the exception object.
            var exc = server.GetLastError();

            // Handle HTTP errors
            if (exc.GetType() == typeof(HttpException))
            {
                // The Complete Error Handling Example generates
                // some errors using URLs with "NoCatch" in them;
                // ignore these here to simulate what would happen
                // if a global.asax handler were not implemented.
                if (exc.Message.Contains("NoCatch") || exc.Message.Contains("maxUrlLength")) return;

                Current.Log.Add(request.Url + ": Redirecting...", Message.EContentType.Info);

                //Redirect HTTP errors to HttpError page

                context.RewritePath("/");
                // Server.Transfer("HttpErrorPage.aspx");
            }

            // For other kinds of errors give the user some information
            // but stay on the default page
            response.Write("<h2>Global Page Error</h2>\n");
            response.Write(
                "<p>" + exc.Message + "</p>\n");
            response.Write("Return to the <a href='Default.aspx'>" +
                           "Default Page</a>\n");

            // Log the exception and notify system operators

            Current.Log.Add(exc);

            // Clear the error from the server
            server.ClearError();
        }
    }
}