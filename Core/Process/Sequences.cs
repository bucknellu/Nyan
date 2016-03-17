using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using System;

namespace Nyan.Core.Process
{
    public static class Sequences
    {
        public static bool IsShuttingDown { get; private set; }

        public static void End(string pReason = "(None)")
        {
            Current.Log.Add("Stack shutdown initiated: " + pReason, Message.EContentType.ShutdownSequence);

            if (IsShuttingDown) return;

            IsShuttingDown = true;

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

            try { System.Windows.Forms.Application.Exit(); } catch { }
            try { Environment.Exit(0); } catch { }
        }
    }
}