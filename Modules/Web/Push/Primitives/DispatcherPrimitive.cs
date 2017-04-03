using System;
using System.Collections.Generic;
using System.Threading;
using Nyan.Core.Settings;
using Nyan.Core.Shared;
using Nyan.Modules.Web.Push.Model;

namespace Nyan.Modules.Web.Push.Primitives
{
    [Priority(Level = -99)]
    public class DispatcherPrimitive
    {
        private readonly Queue<Entry> _messageQueue = new Queue<Entry>();
        private readonly Thread _workerThread;
        private readonly bool _mustCycle = true;
        private int CycleLengthMilliseconds { get; set; } = 5000;


        public DispatcherPrimitive()
        {
            _workerThread = new Thread(DispatcherWorker) { IsBackground = false };
            _workerThread.Start();
        }

        public void Enqueue(EndpointEntry ep, object obj)
        {
            Current.Log.Add("PUSH DispatcherPrimitive: Message enqueued " + ep.endpoint);
            _messageQueue.Enqueue(new Entry { EndpointEntry = ep, Payload = obj });
        }

        private void DispatcherWorker()
        {
            do
            {
                if (_messageQueue.Count == 0) Thread.Sleep(CycleLengthMilliseconds);
                else DispatchQueue();
            } while (_mustCycle);
        }

        private void DispatchQueue()
        {
            while (_messageQueue.Count != 0)
            {
                var a = _messageQueue.Peek();

                try { Send(a.EndpointEntry, a.Payload); }
                catch (Exception e)
                {
                    Current.Log.Add(e, "DispatchQueue:");
                }

                if (_messageQueue.Count != 0) _messageQueue.Dequeue();
            }
        }

        public virtual void Send(EndpointEntry ep, object obj) { }
        public virtual void Register(EndpointEntry ep) { }
        public virtual void Deregister(EndpointEntry ep) { }
        public virtual void HandlePushAttempt(string endpoint, bool success) { }
        private class Entry
        {
            public EndpointEntry EndpointEntry { get; set; }
            public object Payload { get; set; }
        }
    }
}