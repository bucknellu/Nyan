using System;
using System.Threading;

namespace Nyan.Core.Modules.Process
{
    public static class ThreadContext
    {
        public static void Set(string field, object value)
        {
            Thread.SetData(Thread.GetNamedDataSlot(field), value);
        }

        public static T Get<T>(string field)
        {
            try
            {
                var ret = (T)Thread.GetData(Thread.GetNamedDataSlot(field));
                return ret;
            }
            catch (Exception) { return default(T); }
        }
    }
}