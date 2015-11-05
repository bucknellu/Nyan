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

namespace Nyan.Modules.Log.ZeroMQ
{
    public class Channel : IDisposable
    {
        public delegate void MessageArrivedHandler(Message message);

        private readonly string _address;
        private readonly bool _canReceive;
        private readonly bool _canSend;


        private readonly PublisherSocket _publisherSocket;
        private readonly Dictionary<string, Message> _replyRequestMessages = new Dictionary<string, Message>();
        private readonly object _sendLock = new object();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly string _topic;

        public Channel(string topic = "", bool canSend = true, bool canReceive = true, string address = null)
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
                _publisherSocket.Options.MulticastHops = 4;
                _publisherSocket.Options.MulticastRate = 40 * 1024; // 40 megabit
                _publisherSocket.Options.MulticastRecoveryInterval = TimeSpan.FromMinutes(10);
                _publisherSocket.Options.SendBuffer = 1024 * 10; // 10 megabyte

                _publisherSocket.Bind(_address);

                _publisherSocket.SendReady += (s, a) => { Console.WriteLine("************SENDREADY"); };

                //do { Thread.Sleep(100); } while (!_SendReady);
            }


            if (!_canReceive) return;

            Task.Factory.StartNew(MonitorMessages);
            Task.Run(async () => await CleanupAsync());
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
        }

        public event MessageArrivedHandler MessageArrived;

        private void MonitorMessages()
        {
            using (var context = NetMQContext.Create())
            using (var subscriber = context.CreateSubscriberSocket())
            {
                subscriber.Options.ReceivevBuffer = 1024 * 10;
                subscriber.Subscribe(_topic);
                subscriber.Connect(_address);

                subscriber.ReceiveReady += subscriber_ReceiveReady;

                while (!_tokenSource.Token.IsCancellationRequested) //Forever loop until Dispose() is called.
                {
                    var topic = subscriber.Receive();
                    var payload = subscriber.Receive();


                    Message message = null;

                    try { message = payload.FromSerializedBytes<Message>(); }
                    catch { }

                    if (message == null)
                    {
                        try { message = payload.GetString().FromJson<Message>(); }
                        catch { }
                    }

                    if (message == null) continue;

                    if (MessageArrived == null) continue;

                    if (message.ReplyToId != null)
                    {
                        var key = message.ReplyToId.ToString();

                        if (!_replyRequestMessages.ContainsKey(key)) continue;
                        _replyRequestMessages[key] = message;
                    }
                    else
                    {
                        if (message.Topic == "Diagnostics")
                            ProcessLowLevelOperations(message);

                        MessageArrived(message);
                    }
                }
            }
        }

        private void subscriber_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
        }

        private void ProcessLowLevelOperations(Message message)
        {
            if (message.Subject != "Ping?") return;

            var ret = new Message { Subject = "Pong!", ReplyToId = message.Id };
            Send(ret);
        }

        public void Send(string message)
        {
            Send(new Message(message));
        }

        public Message Send(Message message, bool waitReturn = false)
        {
            if (!_canSend)
                throw new InvalidOperationException("Channel was told at construction time it's not allowed to Send.");

            Message ret = null;
            Task<Message> task = null;

            var originalMsgId = message.Id.ToString();

            if (waitReturn)
            {
                _replyRequestMessages.Add(originalMsgId, null);

                task = new Task<Message>(() => WaitReply(originalMsgId));
                task.Start();
            }

            var payload = message.ToJson().GetBytes();

            lock (_sendLock)
            {
                if (message.Topic != "")
                    _publisherSocket.SendMore(_topic).Send(payload);
                else
                    _publisherSocket.Send(payload);
            }

            if (waitReturn)
            {
                task.Wait();
                ret = task.Result;
            }

            return ret;
        }

        private Message WaitReply(string id)
        {
            var timeout = new Stopwatch();
            timeout.Start();


            while (timeout.ElapsedMilliseconds < 2000)
            {
                if (_replyRequestMessages[id] == null)
                {
                    Thread.Sleep(20);
                    continue;
                }


                var ret = _replyRequestMessages[id];
                _replyRequestMessages.Remove(id);
                return ret;
            }

            timeout.Stop();

            _replyRequestMessages.Remove(id);

            return null;
        }

        public async Task CleanupAsync()
        {
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), _tokenSource.Token);
            }
        }
    }
}