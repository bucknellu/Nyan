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
            Nyan.Modules.Web.REST.Sequences.Start();
        }

        public void Application_End()
        {
            Nyan.Modules.Web.REST.Sequences.End();
        }
    }
}