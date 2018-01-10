using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Routing;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST
{
    public class AngularHtml5ModeStaticModule : IHttpModule
    {
        private static readonly List<string> ReplaceablePages = new List<string> {"index.htm", "main.html"};
        private StreamWatcher _watcher;

        // ReSharper disable once EmptyConstructor
        public AngularHtml5ModeStaticModule() { }

        public string ModuleName => "AngularHtml5ModeStaticModule";

        public void Init(HttpApplication application)
        {
            application.BeginRequest += Application_BeginRequest;
            application.PostMapRequestHandler += Application_PostMapRequestHandler;
            application.EndRequest += Application_EndRequest;
        }

        public void Dispose() { }

        private void Application_EndRequest(object sender, EventArgs e) { ReplaceVersionMarkers(HttpContext.Current); }

        private void ReplaceVersionMarkers(HttpContext context)
        {
            var step = "obtaining content type";
            var url = "";
            try
            {
                step = "handling URL";

                url = context.Request.Url.AbsolutePath.ToLower();
                if (ReplaceablePages.All(i => url.IndexOf(i, StringComparison.Ordinal) == -1)) return;

                Current.Log.Add("POSTPROCESS " + url, Message.EContentType.StartupSequence);

                step = "obtaining version";

                //var ver = "fv=" + Assembly.GetExecutingAssembly().GetName().Version;

                //Log.Add("$$FWVER$$ rewrite: {0} (type: {1}), ver: {2}".format(context.Request.Url.ToString(), rt, ver), Message.EContentType.StartupSequence);

                step = "obtaining original content";
                var cntt = _watcher.ToString();

                step = "replacing base URL";
                var ret = cntt.Replace("$$APP_BASE_URL$$", Environment.ShortBaseUrl);

                step = "clearing response output";
                context.Response.Clear();

                step = "replacing response output";
                context.Response.Headers.Remove("Content-Encoding");

                //context.Response.AddHeader("Content-Encoding", "deflate");

                step = "removing cacheability";
                context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

                context.Response.Write(ret);
                //step = "flushing response output";
                //context.Response.End();
            } catch (Exception e) { Current.Log.Add("Post-process failed while {0} for {1}: {2}".format(step, url, e.Message), Message.EContentType.Warning); }
        }

        private void Application_PostMapRequestHandler(object sender, EventArgs e)
        {
            var r = ((HttpApplication) sender).Request;
            var a = r.RequestContext.HttpContext;

            if (a.Handler == null) return;

            var probe = a.Handler.GetType().Name == "TransferRequestHandler";

            if (!probe) return;

            // Create HttpApplication and HttpContext objects to access
            // request and response properties.
            var application = (HttpApplication) sender;
            var context = application.Context;
            var contextBase = context.Request.RequestContext.HttpContext;

            // 0th Test: If it isn't a GET, doesn't even make sense to continue.
            if (application.Request.HttpMethod != "GET") return;

            // 1st Test: Does it map to a route?
            var route = RouteTable.Routes.GetRouteData(contextBase);
            if (route != null) return;

            //2nd Test: Does a file exist?

            var request = application.Request;
            var url = request.Url.LocalPath;
            if (File.Exists(context.Server.MapPath(url))) return;

            // Seems it's a 404. Let's redirect to main app.

            var absPath = request.Url.AbsolutePath.TrimEnd('/') + "/";
            var baseUrl = request.ApplicationPath.TrimEnd('/') + "/";

            if (absPath.ToLower() == baseUrl.ToLower()) return;

            Current.Log.Add("URL: " + request.Url.AbsolutePath, Message.EContentType.Info);
            Current.Log.Add("TO : " + baseUrl, Message.EContentType.Info);

            context.RewritePath(baseUrl);
        }

        private void Application_BeginRequest(object source, EventArgs e)
        {
            _watcher = new StreamWatcher(HttpContext.Current.Response.Filter);
            HttpContext.Current.Response.Filter = _watcher;
        }
    }
}