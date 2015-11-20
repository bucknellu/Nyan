using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NetMQ;
using NetMQ.Sockets;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using AsyncIO;

namespace Nyan.Modules.Log.ZeroMQ
{
    public class Channel : IDisposable
    {
        public delegate void MessageArrivedHandler(Message message);

        private readonly string _address;
        private readonly bool _canReceive;
        private readonly bool _canSend;

        private readonly SafeHandle _handle = new SafeFileHandle(IntPtr.Zero, true);
        private readonly string _topic;
        private bool _disposed;

        private NetMQContext _mqContext;
        private PublisherSocket _publisherSocket;
        private Thread _workerReceiverThread;
        private Thread _workerSenderThread;

        private AsyncIO.AsyncSocket _dummySocket = null; //Necessary because VS is finicky.

        public Channel(string topic = "", bool canSend = true, bool canReceive = false, string address = null)
        {
            _topic = topic;
            _canSend = canSend;
            _canReceive = canReceive;
            _address = address;

            var uri = new Uri(_address);

            Protocol = uri.Scheme;
            Uri = _address;

            if (!(_canSend || _canReceive))
                throw new InvalidOperationException("Channel was told not to Send or Receive.");

            if (_canSend)
                StartSender();

            if (!_canReceive) return;

            MonitorMessages();
        }

        public string Protocol { get; private set; }

        public string Uri { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void StartSender()
        {
            _workerSenderThread = new Thread(SenderWorker) { IsBackground = true };
            _workerSenderThread.Start();
        }

        private void StartListener()
        {
            if (!_canReceive) throw new Exception("Channel initialized with CanReceive set to false.");

            _workerReceiverThread = new Thread(ReceiverWorker) { IsBackground = true };
            _workerReceiverThread.Start();
        }

        private void SenderWorker()
        {
            _mqContext = NetMQContext.Create();
            _publisherSocket = _mqContext.CreatePublisherSocket();

            if (Protocol == "tcp")
                _publisherSocket.Connect(_address);

            if (Protocol == "pgm") //Multicast
            {
                _publisherSocket.Options.MulticastHops = 4;
                _publisherSocket.Options.MulticastRate = 40 * 1024; // 40 megabit
                _publisherSocket.Options.MulticastRecoveryInterval = TimeSpan.FromMinutes(10);
                _publisherSocket.Options.SendBuffer = 1024 * 10; // 10 megabyte
                _publisherSocket.Bind(_address);
            }

            _publisherSocket.SendReady += (s, a) => { };
        }

        public void Terminate()
        {
            if (_publisherSocket != null)
            {
                _publisherSocket.Close();
                _publisherSocket.Dispose();
                _publisherSocket = null;
            }
            if (_mqContext != null)
            {
                _mqContext.Terminate();
                _mqContext = null;
            }

            if (_workerReceiverThread != null)
            {
                _workerReceiverThread.Abort();
                _workerReceiverThread = null;
            }

            if (_workerSenderThread != null)
            {
                _workerSenderThread.Abort();
                _workerSenderThread = null;
            }

        }

        public event MessageArrivedHandler MessageArrived;

        private void MonitorMessages() { StartListener(); }

        private void ReceiverWorker()
        {
            using (var context = NetMQContext.Create())
            using (var subscriber = context.CreateSubscriberSocket())
            {
                subscriber.Options.ReceiveBuffer = 1024 * 10;
                subscriber.Bind(_address);
                subscriber.Subscribe(_topic);

                if (Protocol == "tcp")
                {
                    //_publisherSocket.Connect(_address);
                }

                if (Protocol == "pgm") //Multicast
                    subscriber.Connect(_address);

                var prevMsgId = new Guid();

                while (!_disposed) //Forever loop until Dispose() is called.
                {
                    var topic = subscriber.ReceiveFrameString();
                    var payload = subscriber.ReceiveFrameBytes();

                    Message message = null;

                    try { message = payload.FromSerializedBytes<Message>(); }
                    catch { }

                    if (message == null)
                        try { message = payload.GetString().FromJson<Message>(); }
                        catch { }

                    if (message == null) continue;

                    if (message.Id.Equals(prevMsgId)) continue; // Avoid ZeroMQ duplicates

                    if (MessageArrived == null) continue;

                    prevMsgId = message.Id;

                    MessageArrived(message);
                }
            }
        }

        public void Send(Message message)
        {
            if (!_canSend)
                throw new InvalidOperationException("Channel was told at construction time it's not allowed to Send.");

            var payload = message.ToSerializedBytes();

            if (message.Topic != "")
                _publisherSocket.SendMoreFrame(_topic).SendFrame(payload);
            else
                _publisherSocket.SendFrame(payload);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _handle.Dispose();
                Terminate();

                if (_publisherSocket != null)
                    _publisherSocket.Dispose();
            }

            // Free any unmanaged objects here.
            //
            _disposed = true;
        }
    }
}