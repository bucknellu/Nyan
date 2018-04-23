using System;
using System.Collections.Generic;
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
            Audit = 10,
            Exception = 20,
            StartupSequence = 30,
            ShutdownSequence = 40,
            Warning = 50,
            Maintenance = 60,
            Info = 70,
            MoreInfo = 80,
            Generic = 90,
            Debug = 100
        }

        public static Dictionary<EContentType, ConsoleColor> ContentColors  = new Dictionary<EContentType, ConsoleColor>()
        {
            {EContentType.Audit, ConsoleColor.Green},
            {EContentType.Exception, ConsoleColor.Red},
            {EContentType.StartupSequence, ConsoleColor.Yellow},
            {EContentType.ShutdownSequence, ConsoleColor.Yellow},
            {EContentType.Warning, ConsoleColor.Magenta},
            {EContentType.Maintenance, ConsoleColor.Cyan},
            {EContentType.Info, ConsoleColor.Blue},
            {EContentType.MoreInfo, ConsoleColor.Gray},
            {EContentType.Generic, ConsoleColor.White},
            {EContentType.Debug, ConsoleColor.Cyan},
        };

        #endregion

        #region Exposed Properties

        public EContentType Type = EContentType.Generic;
        public Guid Id { get; set; }
        public DateTime CreationTime { get; set; }
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