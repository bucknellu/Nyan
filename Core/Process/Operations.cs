using System.Threading;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Process
{
    public static class Operations
    {
        private static bool _doShutdown = true;

        private static Thread _workerThread;

        public static void StartTakeDown(int seconds = 30)
        {
            _doShutdown = true;

            if (_workerThread != null) return;

            _workerThread = new Thread(() => DoTakeDown(seconds)) {IsBackground = false};
            _workerThread.Start();
        }

        private static void DoTakeDown(int seconds)
        {
            Current.Log.Add("Scheduling shutdown: {0} seconds".format(seconds), Message.EContentType.Maintenance);

            Thread.Sleep(seconds*1000);

            if (_doShutdown)
            {
                Current.Log.Add("Starting scheduled shutdown", Message.EContentType.Maintenance);
                Thread.Sleep(2*1000);
                Sequences.End("Scheduled shutdown");
            }
        }

        private static void CancelTakeDown()
        {
            if (!_doShutdown)
            {
                Current.Log.Add("CancelTakeDown: No scheduled shutdown", Message.EContentType.Info);
                return;
            }

            _doShutdown = false;
            _workerThread.Abort();

            Current.Log.Add("CancelTakeDown successful.", Message.EContentType.ShutdownSequence);
        }
    }
}