using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using NetMQContext = NetMQ.NetMQContext;
using NetMQSocketEventArgs = NetMQ.NetMQSocketEventArgs;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Nyan.Modules.Log.ZeroMQ
{
    public class Channel: IDisposable
    {
        public delegate void MessageArrivedHandler(Message message);

        private readonly string _address;
        private readonly bool _canReceive;
        private readonly bool _canSend;

        private bool _readyToSend;


        private readonly PublisherSocket _publisherSocket;
        private readonly Dictionary<string, Message> _replyRequestMessages = new Dictionary<string, Message>();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly string _topic;

        public Channel(string topic = "", bool canSend = true, bool canReceive = false, string address = null)
        {
            _topic = topic;
            _canSend = canSend;
            _canReceive = canReceive;
            _address = address;



            if (!(_canSend || _canReceive))
                throw new InvalidOperationException("Channel was told not to Send or Receive.");

            if (_canSend)
            {
                var mqContext = NetMQContext.Create();

                _publisherSocket = mqContext.CreatePublisherSocket();

                var uri = new Uri(_address);

                if (uri.Scheme == "tcp")
                {
                    _publisherSocket.Connect(_address);
                }


                if (uri.Scheme == "pgm")  //Multicast
                {
                    _publisherSocket.Options.MulticastHops = 4;
                    _publisherSocket.Options.MulticastRate = 40 * 1024; // 40 megabit
                    _publisherSocket.Options.MulticastRecoveryInterval = TimeSpan.FromMinutes(10);
                    _publisherSocket.Options.SendBuffer = 1024 * 10; // 10 megabyte
                    _publisherSocket.Bind(_address);
                }

                _publisherSocket.SendReady += (s, a) => { _readyToSend = true; };

                //do { Thread.Sleep(100); } while (!_SendReady);
            }


            if (!_canReceive) return;

            Task.Factory.StartNew(MonitorMessages);
            Task.Run(async () => await CleanupAsync());
        }

        public event MessageArrivedHandler MessageArrived;

        private void MonitorMessages()
        {

            if (!_canReceive) throw new Exception("Channel initialized with CanReceive set to false.");

            using (var context = NetMQContext.Create())
            using (var subscriber = context.CreateSubscriberSocket())
            {
                subscriber.Options.ReceivevBuffer = 1024 * 10;
                subscriber.Bind(_address);
                subscriber.Subscribe(_topic);
                //subscriber.Connect(_address);

                while (!_tokenSource.Token.IsCancellationRequested) //Forever loop until Dispose() is called.
                {
                    var topic = subscriber.ReceiveString();
                    var payload = subscriber.ReceiveString();

                    Message message = null;

                    //try { message = payload.FromSerializedBytes<Message>(); }
                    //catch { }

                    if (message == null)
                    {
                        try { message = payload.FromJson<Message>(); }
                        catch { }
                    }

                    if (message == null) continue;
                    if (MessageArrived == null) continue;

                    MessageArrived(message);
                }
            }
        }

        public Message Send(Message message, bool waitReturn = false)
        {
            if (!_canSend)
                throw new InvalidOperationException("Channel was told at construction time it's not allowed to Send.");

            Message ret = null;

            var originalMsgId = message.Id.ToString();

            var payload = message.ToJson();

            if (message.Topic != "")
                _publisherSocket.SendMore(_topic).Send(payload);
            else
                _publisherSocket.Send(payload);

            return ret;
        }

        public async Task CleanupAsync()
        {
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), _tokenSource.Token);
            }
        }

        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                _tokenSource.Cancel();

                if (_publisherSocket != null)
                {
                    _publisherSocket.Dispose();
                }
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }
    }
}