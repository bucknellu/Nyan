using System;
using System.Diagnostics;
using Nyan.Core.Extensions;

namespace Nyan.Core.Modules.Log
{
    public abstract class LogProvider
    {
        public virtual event Message.MessageArrivedHandler MessageArrived;

        protected virtual void OnMessageArrived(Message message)
        {
            var handler = MessageArrived;
            if (handler != null) handler(message);
        }

        public virtual void Add(bool content)
        {
            Add(content.ToString());
        }

        public virtual void Add(string pattern, params object[] replacementStrings)
        {
            Add(string.Format(pattern, replacementStrings));
        }

        public virtual void Add(Exception e)
        {
            Add(e, null);
        }

        public virtual void Add(Exception e, string message)
        {
            if (message == null)
                message = e.Message;

            var msg = message;

            try { msg = message + " " + new StackTrace(e, true).FancyString(); }
            catch { }

            Add(msg, Message.EContentType.Exception);
        }

        public virtual void Add(string pMessage, Exception e)
        {
            Add(e, pMessage);
        }

        public virtual void StartListening()
        {
        }

        public virtual void Add(string content, Message.EContentType type = Message.EContentType.Generic)
        {
            Dispatch(content, type);
        }

        public virtual void Dispatch(string content, Message.EContentType type = Message.EContentType.Generic)
        {
            Console.WriteLine(content);
            Debug.Print(content);
        }
    }
}