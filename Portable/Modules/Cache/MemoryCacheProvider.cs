using System;
using System.Collections.Generic;
using Nyan.Core.Modules.Cache;
using System.Runtime.Caching;
using Nyan.Core.Extensions;

namespace Nyan.Portable.Modules.Cache
{
    public class MemoryCacheProvider : ICacheProvider
    {
        private MemoryCache cache;

        public MemoryCacheProvider()
        {
            OperationalStatus = EOperationalStatus.Initialized;
            cache = new MemoryCache("NyanCache");
            OperationalStatus = EOperationalStatus.Operational;
        }

        static readonly object oLock = new object();

        public string this[string key, string oSet = null, int cacheTimeOutSeconds = 600]
        {
            get
            {
                var res = cache[key];
                return res == null ? null : res.ToString();
            }

            set
            {
                if (value == null)
                    Remove(key);
                else
                    lock (oLock) { cache.Add(key, value, DateTimeOffset.Now.AddMilliseconds(cacheTimeOutSeconds)); }

            }
        }

        public EOperationalStatus OperationalStatus { get; private set; }

        public bool Contains(string key)
        {
            return cache.Contains(key);
        }

        public IEnumerable<string> GetAll(string oNamespace)
        {
            throw new NotImplementedException();
        }

        public T GetSingleton<T>(string fullName = null)
        {
            var n = (fullName ?? typeof(T).FullName) + ":s";
            var c = this[n];

            return c == null ? default(T) : c.FromJson<T>();
        }

        public void Remove(string key, string oSet = null)
        {
            lock (oLock) { cache.Remove(key); }
        }

        public void SetSingleton(object value, string fullName = null)
        {
            var n = (fullName ?? value.GetType().FullName) + ":s";
            this[n] = value.ToJson();
        }
    }
}
