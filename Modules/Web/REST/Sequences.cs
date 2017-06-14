using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Nyan.Core.Assembly;

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
            else Core.Process.Sequences.End();
        }
    }
}