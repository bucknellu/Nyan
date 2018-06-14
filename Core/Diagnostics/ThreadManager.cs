using System;
using System.Threading;
using Nyan.Core.Extensions;

namespace Nyan.Core.Diagnostics
{
    public class ThreadHelper
    {
        private static readonly ThreadLocal<string> _Uid = new ThreadLocal<string>(() => Guid.NewGuid().ToShortGuid().ToString());
        public static string Uid => _Uid.Value;
    }
}