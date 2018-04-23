using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Nyan.Core.Process;

namespace Nyan.Core.Modules.Log
{
    public abstract class LogProvider
    {
        private static bool _isResetting;

        private bool _shutdown;

        private bool _useScheduler;
        private Thread _workerThread;

        public List<string> ShutdownTriggerTerms = new List<string>();

        public virtual string Protocol => null;

        public virtual string Uri => null;

        public bool UseScheduler
        {
            get => _useScheduler;
            set
            {
                _useScheduler = value;

                if (!_useScheduler)
                {
                    if (_workerThread == null) return;

                    _workerThread.Abort();
                    _workerThread = null;
                }
                else
                {
                    if (_workerThread != null) return;

                    _workerThread = new Thread(DispatcherWorker) {IsBackground = false};
                    _workerThread.Start();
                }
            }
        }

        private void DispatcherWorker()
        {
            while (_useScheduler)
            {
                Local.ManageInventory();
                Thread.Sleep(250);
            }
        }

        public virtual event Message.MessageArrivedHandler MessageArrived;

        public virtual void Shutdown()
        {
            _shutdown = true;
            UseScheduler = false;
        }

        protected virtual void OnMessageArrived(Message message)
        {
            var handler = MessageArrived;
            if (handler != null) handler(message);
        }

        public virtual void Add(bool content) { Add(content.ToString()); }

        public virtual void Add(string pattern, params object[] replacementStrings) { Add(string.Format(pattern, replacementStrings)); }

        public virtual void Add(Exception e) { Add(e, null, null); }

        public virtual void Add(Exception e, string message, string token = null)
        {
            Add(Converter.ToMessage(e));
            if (e.InnerException != null) Add(e.InnerException);
        }

        public virtual void Add(Type t, string message, Message.EContentType type = Message.EContentType.Generic) { Add(t.FullName + " : " + message, type); }

        public virtual void Add(string pMessage, Exception e) { Add(e, pMessage, null); }

        public virtual void StartListening() { }

        public virtual void BeforeDispatch(Message payload) { }
        public virtual void BeforeAdd(Message payload) { }

        public virtual void AfterDispatch(Message payload)
        {
            if (Settings.replicateLocally) System.Add(payload.Content);
        }

        public virtual bool doDispatchCycle(Message payload)
        {
            try
            {
                BeforeDispatch(payload);
                Dispatch(payload);
                AfterDispatch(payload);
                return true;
            } catch { return false; }
        }

        public virtual bool CheckShutdownTriggerTerms(string payload)
        {
            try
            {
                if (_isResetting) return true;

                foreach (var ft in ShutdownTriggerTerms)
                {
                    if (payload.IndexOf(ft, StringComparison.OrdinalIgnoreCase) == -1) continue;

                    _isResetting = true;

                    Add("Fatal trigger exception detected: '" + payload + "'", Message.EContentType.Exception);
                    System.Add("Fatal trigger exception detected: '" + payload + "'");

                    Operations.StartTakeDown(5);

                    return true;
                }

                return false;
            } catch { return false; }
        }

        public virtual void Add(string content, Message.EContentType type = Message.EContentType.Generic)
        {
            var payload = Converter.ToMessage(content, type);

            try { BeforeAdd(payload); } catch { }

            if (_shutdown) return;

            if (string.IsNullOrEmpty(content)) return;

            // if (CheckShutdownTriggerTerms(content)) return;

            CheckShutdownTriggerTerms(content);

            if (type > Settings.VerbosityThreshold) return;

            Add(payload);
        }

        public virtual void Add(Message m) { Local.Store(m); }

        public virtual bool Dispatch(Message payload)
        {
            Console.ForegroundColor = Message.ContentColors[payload.Type];
            Console.WriteLine(payload.Content);
            Debug.Print(payload.Content);

            return true;
        }
    }
}