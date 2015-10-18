using System;
using System.Web.Http;

namespace Nyan.Samples.REST
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(Nyan.Modules.Web.REST.Initialization.Register);
        }
    }
}