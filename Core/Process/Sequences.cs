using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Process
{
    public static class Sequences
    {
        public static void End(string pReason = "(None)")
        {
            Current.Log.Add("Stack shutdown initiated: " + pReason, Message.EContentType.ShutdownSequence);

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