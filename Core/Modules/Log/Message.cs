using System;
using Nyan.Core.Diagnostics;

namespace Nyan.Core.Modules.Log
{
    public class Message
    {
        public delegate void MessageArrivedHandler(object message);

        #region Enumerators

        //Type-safe-enum pattern standard interface

        [Serializable]
        public sealed class EContentType : IEContentType
        {
            public static readonly EContentType Audit = new EContentType(0, "AUD", "Audit");
            public static readonly EContentType Debug = new EContentType(1, "DBG", "Debug");
            public static readonly EContentType Generic = new EContentType(2, "GEN", "Generic");
            public static readonly EContentType Warning = new EContentType(3, "WRN", "Warning");
            public static readonly EContentType Exception = new EContentType(4, "ERR", "Exception");
            public static readonly EContentType Maintenance = new EContentType(5, "MTN", "Maintenance");
            public static readonly EContentType StartupSequence = new EContentType(6, "STA", "Start-up Sequence");

            private EContentType(int value, string code, string name)
            {
                Value = value;
                Name = name;
                Code = code;
            }

            public string Name { get; private set; }
            public string Code { get; private set; }
            public int Value { get; private set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public interface IEContentType
        {
            string Name { get; }
            string Code { get; }
            int Value { get; }
            string ToString();
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