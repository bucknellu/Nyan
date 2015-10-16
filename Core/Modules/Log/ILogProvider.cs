using Nyan.Core.Extensions;
using System;
using System.Diagnostics;

namespace Nyan.Core.Modules.Log
{
    public abstract class ILogProvider
    {
        public event Message.MessageArrivedHandler MessageArrived;

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

            var msg = "(no source)";

            try
            {
                msg = new StackTrace(e, true).FancyString();
            }
            catch { }

            Add(msg, Message.EContentType.Exception);
        }
        public virtual void Add(string pMessage, Exception e)
        {
            Add(e, pMessage);
        }
        public virtual void StartListening() { }

        public virtual void Add(string content, Message.EContentType type = null)
        {
            Console.WriteLine(content);
        }
    }
}
