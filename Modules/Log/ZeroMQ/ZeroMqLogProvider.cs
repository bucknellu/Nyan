using System;
using System.Diagnostics;
using Nyan.Core.Modules.Log;

namespace Nyan.Modules.Log.ZeroMQ
{
    public class ZeroMqLogProvider : LogProvider
    {
        private Channel _in;
        private string _multiCastAddress = "";
        private Channel _out;

        public ZeroMqLogProvider()
        {
            Initialize("pgm://239.255.42.99:5558");
        }

        public ZeroMqLogProvider(string multiCastAddress)
        {
            Initialize(multiCastAddress);
        }

        public new event Message.MessageArrivedHandler MessageArrived;

        private void Initialize(string multiCastAddress)
        {
            _multiCastAddress = multiCastAddress;
            _out = new Channel("Log_Service", true, false, _multiCastAddress);
        }

        public override void Dispatch(string content, Message.EContentType type = Message.EContentType.Generic)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine(content);
                Debug.WriteLine(content);
            }

            var payload = new Message {Content = content, Subject = type.ToString(), Type = type};

            _out.Send(payload);
        }

        public override void StartListening()
        {
            if (_in != null) return;

            _in = new Channel("Log_Service", false, true, _multiCastAddress);
            _in.MessageArrived += _gateway_MessageArrived;
        }

        private void _gateway_MessageArrived(Message message)
        {
            if (MessageArrived == null) return;
            MessageArrived(message);
        }
    }
}