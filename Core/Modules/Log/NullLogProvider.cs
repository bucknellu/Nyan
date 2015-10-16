using Nyan.Core.Extensions;
using System;
using System.Diagnostics;

namespace Nyan.Core.Modules.Log
{
    public class NullLogProvider : ILogProvider
    {
        public new void Add(string content, Message.EContentType type = null)
        {
            Console.WriteLine(content);
        }
    }
}
