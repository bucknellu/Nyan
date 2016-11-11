using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Nyan.Core.Extensions;
using Nyan.Core.Factories;
using Nyan.Core.Process;

namespace Nyan.Core.Modules.Log
{
    public abstract class LogProvider
    {
        private static bool _isResetting;
        private readonly Queue<Message> _messageQueue = new Queue<Message>();
        private bool _shutdown;


        private bool _useScheduler;
        private Thread _workerThread;

        public List<string> ShutdownTriggerTerms = new List<string>();

        public virtual string Protocol { get { return null; } }

        public virtual string Uri { get { return null; } }

        public bool HasMessage { get; private set; } = true;

        public bool UseScheduler
        {
            get { return _useScheduler; }
            set
            {
                _useScheduler = value;

                if (!_useScheduler)
                {
                    if (_workerThread == null) return;

                    _workerThread.Abort();
                    _workerThread = null;
                    DispatchQueue();
                }
                else
                {
                    if (_workerThread != null) return;

                    _workerThread = new Thread(DispatcherWorker) { IsBackground = false };
                    _workerThread.Start();
                }
            }
        }

        private void DispatcherWorker()
        {
            while (_useScheduler)
                if (!HasMessage) Thread.Sleep(100);
                else
                {
                    HasMessage = false;

                    DispatchQueue();
                }
        }

        private void DispatchQueue()
        {
            while (_messageQueue.Count != 0)
            {
                var a = _messageQueue.Peek();

                if (_shutdown) return;

                try
                {
                    doDispatchCycle(a);
                }
                catch (Exception e)
                {
                    System.Add(e); // Log locally.
                }

                if (_messageQueue.Count != 0) _messageQueue.Dequeue();
            }
        }

        public virtual event Message.MessageArrivedHandler MessageArrived;

        public virtual void Shutdown()
        {
            _messageQueue.Clear();
            _shutdown = true;
            UseScheduler = false;
        }

        protected virtual void OnMessageArrived(Message message)
        {
            var handler = MessageArrived;
            if (handler != null) handler(message);
        }

        public virtual void Add(bool content) { Add(content.ToString()); }

        public virtual void Add(string pattern, params object[] replacementStrings)
        {
            Add(string.Format(pattern, replacementStrings));
        }

        public virtual void Add(Exception e) { Add(e, null, null); }

        public virtual void Add(Exception e, string message, string token = null)
        {
            if (token == null) token = Identifier.MiniGuid();

            if (message == null) message = e.Message;

            var ctx = token + " : " + message;

            try
            {
                ctx += " @ " + new StackTrace(e, true).FancyString();
            }
            catch { }

            Add(ctx, Message.EContentType.Exception);

            if (e.InnerException != null) Add(e.InnerException, null, token);
        }

        public virtual void Add(Type t, string message, Message.EContentType type = Message.EContentType.Generic)
        {
            Add(t.FullName + " : " + message, type);
        }

        public virtual void Add(string pMessage, Exception e) { Add(e, pMessage, null); }

        public virtual void StartListening() { }

        public virtual void BeforeDispatch(Message payload) { }

        public virtual void AfterDispatch(Message payload)
        {
            if (Settings.replicateLocally) System.Add(payload.Content);
        }

        public virtual void doDispatchCycle(Message payload)
        {
            try
            {
                BeforeDispatch(payload);
            }
            catch { }
            try
            {
                Dispatch(payload);
            }
            catch { }
            try
            {
                AfterDispatch(payload);
            }
            catch { }
        }

        public virtual bool
            CheckShutdownTriggerTerms(string payload)
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

                    Operations.StartTakeDown(10);

                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public virtual void Add(string content, Message.EContentType type = Message.EContentType.Generic)
        {
            if (_shutdown) return;

            //if (CheckShutdownTriggerTerms(content)) return;

            CheckShutdownTriggerTerms(content);

            if (type > Settings.VerbosityThreshold) return;

            var payload = new Message { Content = content, Subject = type.ToString(), Type = type };

            if (_useScheduler)
            {
                _messageQueue.Enqueue(payload);
                HasMessage = true;
            }
            else
            {
                try
                {
                    doDispatchCycle(payload);
                }
                catch (Exception e)
                {
                    System.Add(e);
                }
            }
        }

        public virtual void Dispatch(Message payload)
        {
            Console.WriteLine(payload.Content);
            Debug.Print(payload.Content);
        }
    }
}