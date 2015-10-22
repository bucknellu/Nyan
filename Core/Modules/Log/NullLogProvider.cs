using System;
using System.Diagnostics;
using Nyan.Core.Extensions;

namespace Nyan.Core.Modules.Log
{
    public class NullLogProvider : LogProvider
    {
        public new void Add(string content, Message.EContentType type = null)
        {
            Debug.Write("[{0}] {1}".format(type.Code, content));
            Console.WriteLine("[{0}] {1}".format(type.Code, content));
        }
    }
}