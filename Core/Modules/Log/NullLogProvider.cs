using System;
using System.Diagnostics;
using Nyan.Core.Extensions;

namespace Nyan.Core.Modules.Log
{
    public class NullLogProvider : LogProvider
    {
        public new void Dispatch(Message payload)
        {
            Debug.Write("[{0}] {1}".format(payload.Type.ToString(), payload.Content));
            Console.WriteLine("[{0}] {1}".format(payload.Type.ToString(), payload.Content));
        }
    }
}