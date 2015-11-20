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

                Current.Log.Add("Stack shutdown initiated: " + shutDownMessage.Replace(Environment.NewLine, ":").Split(':')[0], Message.EContentType.ShutdownSequence);
            }
            else
                Current.Log.Add("Stack shutdown started", Message.EContentType.ShutdownSequence);

            if (Current.Log.UseScheduler)
            {
                Current.Log.UseScheduler = false;
                Current.Log.Add("Log Scheduler switched off", Message.EContentType.MoreInfo);
            }

            Current.Log.Add("Shutting down " + Current.Authorization.GetType().Name, Message.EContentType.MoreInfo);
            Current.Authorization.Shutdown();

            Current.Log.Add("Shutting down " + Current.Cache.GetType().Name, Message.EContentType.MoreInfo);
            Current.Cache.Shutdown();

            Current.Log.Add("Shutting down " + Current.Scope.GetType().Name, Message.EContentType.MoreInfo);
            Current.Scope.Shutdown();

            Current.Log.Add("Shutting down " + Current.Encryption.GetType().Name, Message.EContentType.MoreInfo);
            Current.Encryption.Shutdown();

            Current.Log.Add(@"  _|\_/|  ZZZzzz", Message.EContentType.Info);
            Current.Log.Add(@"c(_(-.-)", Message.EContentType.Info);

            Current.Log.Add(@"Stack shutdown concluded.", Message.EContentType.ShutdownSequence);

            Current.Log.Shutdown();
        }
    }
}