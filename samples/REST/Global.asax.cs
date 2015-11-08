using Nyan.Core.Extensions;
using System;
using System.Reflection;
using System.Web;
using System.Web.Http;

namespace Nyan.Samples.REST
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(Nyan.Modules.Web.REST.Initialization.Register);
            Nyan.Core.Settings.Current.Log.Add("Start!");
        }

        public void Application_End()
        {

            HttpRuntime runtime = (HttpRuntime)typeof(System.Web.HttpRuntime).InvokeMember("_theRuntime",
                                                                                            BindingFlags.NonPublic
                                                                                            | BindingFlags.Static
                                                                                            | BindingFlags.GetField,
                                                                                            null,
                                                                                            null,
                                                                                            null);

            if (runtime == null)
                return;

            string shutDownMessage = (string)runtime.GetType().InvokeMember("_shutDownMessage",
                                                                             BindingFlags.NonPublic
                                                                             | BindingFlags.Instance
                                                                             | BindingFlags.GetField,
                                                                             null,
                                                                             runtime,
                                                                             null);

            string shutDownStack = (string)runtime.GetType().InvokeMember("_shutDownStack",
                                                                           BindingFlags.NonPublic
                                                                           | BindingFlags.Instance
                                                                           | BindingFlags.GetField,
                                                                           null,
                                                                           runtime,
                                                                           null);


            Nyan.Core.Settings.Current.Log.Add("Shutting down: {0} : {1}".format(shutDownMessage, shutDownStack));
        }
    }
}