using System;
using System.Diagnostics;
using Nyan.Core.Extensions;

namespace Nyan.Core.Modules.Log
{
    public class NullLogProvider : LogProvider
    {
        public new void Dispatch(string content, Message.EContentType type = Message.EContentType.Generic)
        {
            Debug.Write("[{0}] {1}".format(type.ToString(), content));
            Console.WriteLine("[{0}] {1}".format(type.ToString(), content));
        }
    }
}