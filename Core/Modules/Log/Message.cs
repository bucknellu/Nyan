using System;
using Nyan.Core.Diagnostics;

namespace Nyan.Core.Modules.Log
{
    [Serializable]
    public class Message
    {
        public delegate void MessageArrivedHandler(Message message);

        #region Enumerators

        //Type-safe-enum pattern standard interface

        public enum EContentType
        {
            Audit,
            Debug,
            Generic,
            Warning,
            Exception,
            Maintenance,
            StartupSequence,
            Info,
            MoreInfo
        }

        #endregion

        #region Exposed Properties

        public EContentType Type = EContentType.Generic;
        public Guid Id { get; private set; }
        public DateTime CreationTime { get; private set; }

        public string Content { get; set; }
        public Guid? ReplyToId { get; set; }
        public TraceInfoContainer TraceInfo { get; set; }
        public string Topic { get; set; }
        public string Subject { get; set; }

        #endregion

        #region Initialization

        public Message()
        {
            Initialize();
        }

        public Message(string message)
        {
            Initialize();
            Content = message;
        }

        private void Initialize()
        {
            Id = Guid.NewGuid();
            TraceInfo = new TraceInfoContainer();
            CreationTime = DateTime.Now;
            TraceInfo.Gather();
        }

        #endregion
    }
}