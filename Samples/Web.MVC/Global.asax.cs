﻿using System.Web.Routing;

namespace Nyan.WebSample
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // AreaRegistration.RegisterAllAreas();
            // FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            // BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}