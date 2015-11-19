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
            Initialize("tcp://127.0.0.1:5003");
        }

        public override void Shutdown()
        {
            Core.Settings.Current.Log.Add("Shutting down Log");

            if (_workerThread != null)
            {
                Core.Settings.Current.Log.Add("Disposing of Listener");
                _mustStop = true;
                _workerThread.Abort();
                _receiver.Close();
                _receiver = null;

            }
            Core.Settings.Current.Log.Add("Disposing of Sender");
            _sender.Close();
            _sender = null;
        }

        public UdpLogProvider(string targetAddress) { Initialize(targetAddress); }

        private void Initialize(string targetAddress)
        {
            _targetAddress = targetAddress;

            var a = new Uri(_targetAddress);
            var ep = new IPEndPoint(IPAddress.Parse(a.Host), a.Port);

            var client = new UdpClient();
            client.Connect(ep);
        }

        public override void Dispatch(Message payload)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine(payload.Content);
                Debug.WriteLine(payload.Content);
            }

            var bytePayload = payload.ToSerializedBytes();

            _sender.Send(bytePayload, bytePayload.Length);
        }

        public override void StartListening()
        {
            _workerThread = new Thread(ListeningWorker) { IsBackground = true, Priority = ThreadPriority.Lowest };
            _workerThread.Start();
        }

        private void ListeningWorker()
        {
            var a = new Uri(_targetAddress);

            _receiver = new UdpClient(a.Port);

            var remoteEp = new IPEndPoint(IPAddress.Any, a.Port);

            while (!_mustStop)
            {
                var data = _receiver.Receive(ref remoteEp); 
                var payload = data.FromSerializedBytes<Message>();

                DoMessageArrived(payload);
            }

        }

        private void DoMessageArrived(Message message)
        {
            if (MessageArrived == null) return;
            MessageArrived(message);
        }

    }
}