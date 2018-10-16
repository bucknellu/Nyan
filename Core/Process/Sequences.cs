using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nyan.Core.Settings;
using Message = Nyan.Core.Modules.Log.Message;

namespace Nyan.Core.Process
{
    public static class Sequences
    {
        public static List<Action> BootstrapActions = new List<Action>();
        public static List<Action> ShutdownActions = new List<Action>();

        public static bool IsShuttingDown { get; private set; }

        public static void Start()
        {
            foreach (var ba in BootstrapActions)
            {
                try { ba(); }
                catch (Exception e) { Current.Log.Add(e); }
            }
        }

        public static void Stop()
        {
            foreach (var sa in ShutdownActions)
            {
                try { sa(); }
                catch (Exception e) { Current.Log.Add(e); }
            }
        }

        public static void End(string pReason = "(None)")
        {
            Current.Log.Add("Stack shutdown initiated: " + pReason, Message.EContentType.ShutdownSequence);

            if (IsShuttingDown) return;

            IsShuttingDown = true;

            Stop();

            Current.Log.Add("Shutting down " + Current.Authorization.GetType().Name, Message.EContentType.MoreInfo);
            Current.Authorization.Shutdown();

            Current.Log.Add("Shutting down " + Current.Cache.GetType().Name, Message.EContentType.MoreInfo);
            Current.Cache.Shutdown();

            Current.Log.Add("Shutting down " + Current.Environment.GetType().Name, Message.EContentType.MoreInfo);
            Current.Environment.Shutdown();

            Current.Log.Add("Shutting down " + Current.Encryption.GetType().Name, Message.EContentType.MoreInfo);
            Current.Encryption.Shutdown();

            Current.Log.Add(@"  _|\_/|  ZZZzzz", Message.EContentType.Info);
            Current.Log.Add(@"c(_(-.-)", Message.EContentType.Info);

            Current.Log.Add(@"Stack shutdown concluded.", Message.EContentType.ShutdownSequence);

            Current.Log.Shutdown();

            try { Application.Exit(); }
            catch { }
            try { Environment.Exit(0); }
            catch { }
        }
    }
}