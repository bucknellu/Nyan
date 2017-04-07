using System;
using System.Diagnostics;
using System.Threading;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Startup
{
    public abstract class ServiceDescriptor
    {
        internal Timer Timer;
        private Thread _workerThread;
        private Stopwatch _stopwatch;

        public ServiceDescriptorConfig Config = new ServiceDescriptorConfig();

        public void Initialize()
        {
            Current.Log.Add("   Scheduled [" + this.Config.Name + "] : starting in " + Config.StartTimeSpan, Message.EContentType.Maintenance);
            Timer = new Timer(Scheduler, null, (int)Config.StartTimeSpan.TotalMilliseconds, Timeout.Infinite);
        }

        public void Scheduler(object state)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            Current.Log.Add("    Starting [" + this.Config.Name + "] cycle", Message.EContentType.Maintenance);
            Process();
            _stopwatch.Stop();
            Current.Log.Add("    Finished [" + this.Config.Name + "] cycle, " + _stopwatch.Elapsed, Message.EContentType.Maintenance);
            Current.Log.Add("    Schedule [" + this.Config.Name + "] : " + Config.CycleTimeSpan, Message.EContentType.Maintenance);
            Timer.Change((int)Config.CycleTimeSpan.TotalMilliseconds, Timeout.Infinite);
        }

        public virtual void Process() { }

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