using NamedPipeWrapper;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using System;
using System.Diagnostics;
using System.IO.Pipes;

namespace Nyan.Modules.Log.NamedPipes
{

    public class NamedPipesLogProvider : LogProvider
    {
        private NamedPipeServer<string> _in;
        private NamedPipeClient<string> _out;

        private PipeSecurity _ps;

        private string _targetAddress = "";

        public NamedPipesLogProvider()
        {
            Initialize("NYAN_LOG_PIPE");
        }
        public NamedPipesLogProvider(string targetAddress) { Initialize(targetAddress); }

        public override string Protocol { get { return null; } }

        public override string Uri { get { return _targetAddress; } }

        public override void Shutdown()
        {
            //if (_in != null) _in.Terminate();
            //if (_out != null) _out.Terminate();
        }

        public override event Message.MessageArrivedHandler MessageArrived;

        private void Initialize(string targetAddress)
        {
            try
            {

                _targetAddress = targetAddress;

                StartListening(); //Required - pipe needs to exist

                _out = new NamedPipeClient<string>(_targetAddress);
                _out.Start();

                UseScheduler = true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override bool Dispatch(Message payload)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine(payload.Content);
                Debug.WriteLine(payload.Content);
            }

            var transformedPayload = payload.ToJson().Encrypt();

            _out.PushMessage(transformedPayload);

            //there is more than 1 server present
            return true;
        }

        public override void StartListening()
        {

            if (_in != null) return;

            _in = new NamedPipeServer<string>(_targetAddress);
            _in.ClientMessage += _in_ClientMessage;
            _in.Start();
        }

        private void _in_ClientMessage(NamedPipeConnection<string, string> connection, string message)
        {
            var _out = message.Decrypt().FromJson<Message>();
            if (_out == null) return;
            MessageArrived(_out);
        }

        private void _gateway_MessageArrived(Message message)
        {
            if (MessageArrived == null) return;
            MessageArrived(message);
        }
    }
}

