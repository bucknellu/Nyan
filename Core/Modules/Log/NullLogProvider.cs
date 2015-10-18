using Nyan.Core.Extensions;
using System;
using System.Diagnostics;

namespace Nyan.Core.Modules.Log
{
    public class NullLogProvider : ILogProvider
    {
        public new void Add(string content, Message.EContentType type = null)
        {
            Debug.Write("[{0}] {1}".format(type.Code, content));
            Console.WriteLine("[{0}] {1}".format(type.Code, content));
        }
    }
}
