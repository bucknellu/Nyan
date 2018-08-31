using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;

namespace Nyan.Modules.Log.UDP
{
    public class UdpLogProvider : LogProvider
    {
        private bool _mustStop;
        private UdpClient _receiver;
        private UdpClient _sender;
        private string _targetAddress = "";
        private Thread _workerThread;

        public UdpLogProvider()
        {
            //tcp://127.0.0.1:5002
            //pgm://239.255.42.99:5558
            Initialize("tcp://127.0.0.1:6000");
        }

        public UdpLogProvider(string targetAddress) { Initialize(targetAddress); }

        public override event Message.MessageArrivedHandler MessageArrived;

        public override void Shutdown()
        {
            if (_workerThread != null)
            {
                _mustStop = true;
                _workerThread.Abort();
                _receiver.Close();
                _receiver = null;
            }

            _sender.Close();
            _sender = null;
        }

        private void Initialize(string targetAddress)
        {
            UseScheduler = true;

            try
            {
                _targetAddress = targetAddress;

                var a = new Uri(_targetAddress);
                var ep = new IPEndPoint(IPAddress.Parse(a.Host), a.Port);

                _sender = new UdpClient
                {
                    Client =
                    {
                        ReceiveBufferSize = 2048,
                        SendBufferSize = 32768
                    }
                };
                _sender.Connect(ep);
            } catch (Exception e)
            {
                // Use the system log, since errors here mean that we won't be able to send it remotely.
                Core.Modules.Log.System.Add(e);
            }
        }

        public override void BeforeAdd(Message payload)
        {

            try
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine(payload.Content);
                }

                var bytePayload = payload.ToJson().Encrypt().ToSerializedBytes();
                //var bytePayload = payload.ToJson().ToSerializedBytes();

                _sender.Send(bytePayload, bytePayload.Length);
            }
            catch (Exception)
            {
                // ignored
            }

        }

        public override void StartListening()
        {
            try
            {
                if (_receiver != null) return;

                _workerThread = new Thread(ListeningWorker) {IsBackground = true, Priority = ThreadPriority.Lowest};
                _workerThread.Start();
            } catch (Exception e)
            {
                // Use the system log, since errors here mean that we won't be able to send it remotely.
                Core.Modules.Log.System.Add(e);
            }
        }

        private void ListeningWorker()
        {
            try
            {
                var errPayload = new Message
                {
                    Type = Message.EContentType.Warning,
                    Content = "(Encrypted / non-desserializable content received)"
                };

                var a = new Uri(_targetAddress);
                var ep = new IPEndPoint(IPAddress.Any, a.Port);

                _receiver = new UdpClient();
                _receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                _receiver.Client.ReceiveBufferSize = 65535;
                _receiver.Client.SendBufferSize = 2048;

                _receiver.Client.Bind(ep);

                while (!_mustStop)
                    try
                    {
                        var data = _receiver.Receive(ref ep);
                        var strData = data.FromSerializedBytes<string>();
                        var payload = strData.Decrypt().FromJson<Message>();

                        DoMessageArrived(payload);
                    } catch { DoMessageArrived(errPayload); }
            } catch (Exception e)
            {
                // Use the system log, since errors here mean that we won't be able to send it remotely.
                Core.Modules.Log.System.Add(e);
            }
        }

        private void DoMessageArrived(Message message)
        {
            if (MessageArrived == null) return;
            MessageArrived(message);
        }
    }
}