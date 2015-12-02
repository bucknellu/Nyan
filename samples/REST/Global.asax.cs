using System;
using System.Web;

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