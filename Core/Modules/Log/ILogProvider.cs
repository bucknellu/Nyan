using System;

namespace Nyan.Core.Modules.Log
{
    public interface ILogProvider
    {
        Message.EContentType MaximumLogLevel { get; set; }
        string Protocol { get; }
        string Uri { get; }
        bool UseScheduler { get; set; }

        event Message.MessageArrivedHandler MessageArrived;

        void Add(bool content);
        void Add(Exception e);
        void Add(Exception e, string message, string token = null);
        void Add(Exception[] es);
        void Add(Message m);
        void Add(string content, Message.EContentType type = Message.EContentType.Generic);
        void Add(string pMessage, Exception e);
        void Add(string pattern, params object[] replacementStrings);
        void Add(Type t, string message, Message.EContentType type = Message.EContentType.Generic);
        void AfterDispatch(Message payload);
        void BeforeAdd(Message payload);
        void BeforeDispatch(Message payload);
        bool CheckShutdownTriggerTerms(string payload);
        bool Dispatch(Message payload);
        bool doDispatchCycle(Message payload);
        void Info(string content);
        void Maintenance(string content);
        void Shutdown();
        void StartListening();
        void Warn(string content);
    }
}