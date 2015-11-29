using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Nyan.Core.Extensions;

namespace Nyan.Core.Modules.Log
{
    public abstract class LogProvider
    {
        public Message.EContentType VerbosityThreshold = Message.EContentType.Debug;
        private bool _Shutdown;
        private bool _hasMessage = true;
        private Queue<Message> _messageQueue = new Queue<Message>();

        private bool _useScheduler;
        private Thread _workerThread;

        public virtual string Protocol { get { return null; } }

        public virtual string Uri { get { return null; } }

        public bool HasMessage { get { return _hasMessage; } }

        public bool UseScheduler
        {
            get { return _useScheduler; }
            set
            {
                _useScheduler = value;

                if (!_useScheduler)
                {
                    if (_workerThread == null)
                        return;

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
            {
                if (!_hasMessage)
                    Thread.Sleep(100);
                else
                {
                    _hasMessage = false;

                    DispatchQueue();

                }
            }
        }

        private void DispatchQueue()
        {
            while (_messageQueue.Count != 0)
            {
                var a = _messageQueue.Peek();

                if (_Shutdown) return;

                try
                {
                    Dispatch(a);
                }
                catch (Exception e)
                {
                    //Something very wrong happened: Can't send the messages. What to do...
                }

                _messageQueue.Dequeue();

            }
        }

        public virtual event Message.MessageArrivedHandler MessageArrived;

        public virtual void Shutdown()
        {
            _messageQueue.Clear();
            _Shutdown = true;
            UseScheduler = false;
        }

        protected virtual void OnMessageArrived(Message message)
        {
            var handler = MessageArrived;
            if (handler != null) handler(message);
        }

        public virtual void Add(bool content) { Add(content.ToString()); }

        public virtual void Add(string pattern, params object[] replacementStrings) { Add(string.Format(pattern, replacementStrings)); }

        public virtual void Add(Exception e) { Add(e, null); }

        public virtual void Add(Exception e, string message)
        {
            if (message == null)
                message = e.Message;

            var msg = message;

            try { msg = message + " " + new StackTrace(e, true).FancyString(); }
            catch { }

            Add(msg, Message.EContentType.Exception);
        }

        public virtual void Add(string pMessage, Exception e) { Add(e, pMessage); }

        public virtual void StartListening() { }

        public virtual void Add(string content, Message.EContentType type = Message.EContentType.Generic)
        {
            if (_Shutdown) return;

            if (type > VerbosityThreshold) return;

            var payload = new Message { Content = content, Subject = type.ToString(), Type = type };

            if (_useScheduler)
            {
                _messageQueue.Enqueue(payload);
                _hasMessage = true;
            }
            else
            {
                try
                {
                    Dispatch(payload);
                }
                catch { }
            }
        }

        public virtual void Dispatch(Message payload)
        {
            Console.WriteLine(payload.Content);
            Debug.Print(payload.Content);
        }
    }
}