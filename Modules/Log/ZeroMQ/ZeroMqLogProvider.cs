using System;
using System.Diagnostics;
using Nyan.Core.Modules.Log;

namespace Nyan.Modules.Log.ZeroMQ
{
    public class ZeroMqLogProvider : LogProvider
    {
        private Channel _in;
        private Channel _out;
        private string _targetAddress = "";

        AsyncIO.AsyncSocket _fakeRefReally = null; // Necessary for proper assembly reference. Silly, right?

        public ZeroMqLogProvider()
        {
            //tcp://127.0.0.1:5002
            //pgm://239.255.42.99:5558
            Initialize("tcp://127.0.0.1:5002");
        }

        public ZeroMqLogProvider(string targetAddress) { Initialize(targetAddress); }

        public override string Protocol { get { return (_out != null ? _out.Protocol : (_in != null ? _in.Protocol : null)); } }

        public override string Uri { get { return (_out != null ? _out.Uri : (_in != null ? _in.Uri : null)); } }

        public override void Shutdown()
        {
            //if (_in != null) _in.Terminate();
            //if (_out != null) _out.Terminate();
        }

        public override event Message.MessageArrivedHandler MessageArrived;

        private void Initialize(string targetAddress)
        {
            _targetAddress = targetAddress;
            _out = new Channel("Log_Service", true, false, _targetAddress);
            UseScheduler = true;
        }

        public override bool Dispatch(Message payload)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine(payload.Content);
                Debug.WriteLine(payload.Content);
            }

            try { beforeSend(payload); } catch (Exception e) { Core.Settings.Current.Log.Add(e); }

            _out.Send(payload);

            return true;
        }

        public virtual void beforeSend(Message payload) { }

        public override void StartListening()
        {
            if (_in != null) return;

            _in = new Channel("Log_Service", false, true, _targetAddress);
            _in.MessageArrived += _gateway_MessageArrived;
        }

        private void _gateway_MessageArrived(Message message)
        {
            if (MessageArrived == null) return;
            MessageArrived(message);
        }
    }
}