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
        private string _targetAddress = "";
        private UdpClient _sender = null;
        private UdpClient _receiver = null;
        private Thread _workerThread = null;
        private bool _mustStop = false;

        public override event Message.MessageArrivedHandler MessageArrived;

        public UdpLogProvider()
        {
            //tcp://127.0.0.1:5002
            //pgm://239.255.42.99:5558
            Initialize("tcp://127.0.0.1:6000");
        }

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

        public UdpLogProvider(string targetAddress) { Initialize(targetAddress); }

        private void Initialize(string targetAddress)
        {

            UseScheduler = true;

            try
            {
                _targetAddress = targetAddress;

                var a = new Uri(_targetAddress);
                var ep = new IPEndPoint(IPAddress.Parse(a.Host), a.Port);

                _sender = new UdpClient();
                _sender.Client.ReceiveBufferSize = 2048;
                _sender.Client.SendBufferSize = 32768;
                _sender.Connect(ep);
            }
            catch (Exception e)
            {
                // Use the system log, since errors here mean that we won't be able to send it remotely.
                Core.Modules.Log.System.Add(e);
            }

        }

        public override void Dispatch(Message payload)
        {
            try
            {

                if (Environment.UserInteractive)
                {
                    Console.WriteLine(payload.Content);
                    Debug.WriteLine(payload.Content);
                }

                try
                {
                    beforeSend(payload);
                }
                catch (Exception e)
                {
                    // Use the system log, since errors here mean that we won't be able to send it remotely.
                    Core.Modules.Log.System.Add(e);
                }

                var bytePayload = payload.ToJson().Encrypt().ToSerializedBytes();
                //var bytePayload = payload.ToJson().ToSerializedBytes();

                _sender.Send(bytePayload, bytePayload.Length);
            }
            catch (Exception e)
            {
                // Use the system log, since errors here mean that we won't be able to send it remotely.
                Core.Modules.Log.System.Add(e);
            }
        }

        public virtual void beforeSend(Message payload) { }

        public override void StartListening()
        {
            try
            {
                if (_receiver != null) return;

                _workerThread = new Thread(ListeningWorker) { IsBackground = true, Priority = ThreadPriority.Lowest };
                _workerThread.Start();
            }
            catch (Exception e)
            {
                // Use the system log, since errors here mean that we won't be able to send it remotely.
                Core.Modules.Log.System.Add(e);
            }

        }

        private void ListeningWorker()
        {
            try
            {
                var errPayload = new Message();
                errPayload.Type = Message.EContentType.Warning;
                errPayload.Content = "(Encrypted / non-desserializable content received)";

                var a = new Uri(_targetAddress);
                var ep = new IPEndPoint(IPAddress.Any, a.Port);

                _receiver = new UdpClient();
                _receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                _receiver.Client.ReceiveBufferSize = 65535;
                _receiver.Client.SendBufferSize = 2048;

                _receiver.Client.Bind(ep);

                while (!_mustStop)
                {

                    try
                    {
                        var data = _receiver.Receive(ref ep);
                        var strData = data.FromSerializedBytes<string>();
                        var payload = strData.Decrypt().FromJson<Message>();

                        DoMessageArrived(payload);
                    }
                    catch
                    {
                        DoMessageArrived(errPayload);
                    }

                }
            }
            catch (Exception e)
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