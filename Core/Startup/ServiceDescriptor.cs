using System;
using System.Diagnostics;
using System.Threading;
using Nyan.Core.Modules.Log;
using Nyan.Core.Modules.Maintenance;
using Nyan.Core.Settings;

namespace Nyan.Core.Startup
{
    public abstract class ServiceDescriptor
    {
        private Stopwatch _stopwatch;
        private Thread _workerThread;

        public ServiceDescriptorConfig Config = new ServiceDescriptorConfig();
        internal Timer Timer;

        public void Initialize()
        {
            Current.Log.Add("   Scheduled [" + Config.Name + "] : starting in " + Config.StartTimeSpan, Message.EContentType.Maintenance);

            Timer = new Timer(Scheduler, null, (int)Config.StartTimeSpan.TotalMilliseconds, Timeout.Infinite);
        }

        public void Scheduler(object state)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            Current.Log.Add("    Starting [" + Config.Name + "] cycle", Message.EContentType.Maintenance);

            try
            {
                Factory.DoMaintenance();
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
            }

            _stopwatch.Stop();

            Current.Log.Add("    Finished [" + Config.Name + "] cycle, " + _stopwatch.Elapsed, Message.EContentType.Maintenance);
            Current.Log.Add("    Schedule [" + Config.Name + "] : " + Config.CycleTimeSpan, Message.EContentType.Maintenance);

            Timer.Change((int)Config.CycleTimeSpan.TotalMilliseconds, Timeout.Infinite);
        }

        public void Start()
        {
            _workerThread = new Thread(Initialize) { IsBackground = false };
            _workerThread.Start();
        }

        public void Stop()
        {
            if (_workerThread == null) return;

            _workerThread.Abort();
            _workerThread = null;
        }

        public class ServiceDescriptorConfig
        {
            public string Description;
            public string Name;
            public TimeSpan StartTimeSpan { get; set; } = TimeSpan.FromSeconds(10);
            public TimeSpan CycleTimeSpan { get; set; } = TimeSpan.FromMinutes(10);
        }
    }
}