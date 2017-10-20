using System;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Log
{
    public static class System
    {
        // Only redirects after multi-step queue refactoring

        public static void Add(Exception e)
        {
            Local.Store(Converter.ToMessage(e));

            Current.Log?.Add(e);
        }

        public static void Add(string message, Message.EContentType type = Message.EContentType.Generic) { Local.Store(Converter.ToMessage(message, type)); }
    }
}