using System;
using System.Reflection;
using System.Web;
using System.Web.Http;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Modules.Web.REST;

namespace Nyan.Samples.REST
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(Initialization.Register);

            foreach (var item in CustomDirectRouteProvider.Routes)
            {
                Current.Log.Add("           " + item, Message.EContentType.Info);
            }
        }

        public void Application_End()
        {
            var runtime = (HttpRuntime) typeof (HttpRuntime).InvokeMember("_theRuntime",
                BindingFlags.NonPublic
                | BindingFlags.Static
                | BindingFlags.GetField,
                null,
                null,
                null);

            if (runtime == null)
                return;

            var shutDownMessage = (string) runtime.GetType().InvokeMember("_shutDownMessage",
                BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.GetField,
                null,
                runtime,
                null);

            var shutDownStack = (string) runtime.GetType().InvokeMember("_shutDownStack",
                BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.GetField,
                null,
                runtime,
                null);


            Current.Log.Add("Shutting down: {0} : {1}".format(shutDownMessage, shutDownStack));
        }
    }
}