using System;
using System.Reflection;
using System.Web;
using System.Web.Http;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST
{
    public static class Sequences
    {
        public static void Start()
        {
            GlobalConfiguration.Configure(Initialization.Register);

            foreach (var item in CustomDirectRouteProvider.Routes)
            {
                Current.Log.Add(item.Value.Method.PadLeft(17) + " : " + item.Key, Message.EContentType.MoreInfo);
            }
        }

        public static void End()
        {
            var runtime = (HttpRuntime)typeof(HttpRuntime).InvokeMember("_theRuntime",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField,
                null, null, null);

            if (runtime != null)
            {
                var shutDownMessage = (string)runtime.GetType().InvokeMember("_shutDownMessage",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
                    null, runtime, null);

                var shutDownStack = (string)runtime.GetType().InvokeMember("_shutDownStack",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
                    null, runtime, null);

                Current.Log.Add("Nyan shutdown started: " + shutDownMessage.Replace(Environment.NewLine, ":").Split(':')[0], Message.EContentType.ShutdownSequence);
            }
            else
                Current.Log.Add("Nyan shutdown started", Message.EContentType.ShutdownSequence);

            if (Current.Log.UseScheduler)
            {
                Current.Log.UseScheduler = false;
                Current.Log.Add("Log Scheduler switched off", Message.EContentType.MoreInfo);
            }

            Current.Authorization.Shutdown();
            Current.Cache.Shutdown();
            Current.Scope.Shutdown();
            Current.Encryption.Shutdown();

            Current.Log.Add(@"  _|\_/|  ZZZzzz", Message.EContentType.ShutdownSequence);
            Current.Log.Add(@"c(_(-.-)", Message.EContentType.ShutdownSequence);
            Current.Log.Add(@"Nyan shutdown concluded.", Message.EContentType.ShutdownSequence);

            Current.Log.Shutdown();
        }
    }
}