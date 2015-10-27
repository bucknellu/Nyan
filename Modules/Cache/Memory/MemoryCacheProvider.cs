using Nyan.Core.Extensions;
using Nyan.Core.Modules.Cache;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace Nyan.Modules.Cache.Memory
{
    public class MemoryCacheProvider : ICacheProvider
    {
        private MemoryCache _cache;

        public MemoryCacheProvider()
        {
            OperationalStatus = EOperationalStatus.Initialized;
            _cache = new MemoryCache("NyanCache");
            OperationalStatus = EOperationalStatus.Operational;
        }

        static readonly object OLock = new object();

        public string this[string key, string oSet = null, int cacheTimeOutSeconds = 600]
        {
            get
            {
                var res = _cache[key];
                return res == null ? null : res.ToString();
            }

            set
            {
                if (value == null)
                    Remove(key);
                else
                    lock (OLock) { _cache.Add(key, value, DateTimeOffset.Now.AddMilliseconds(cacheTimeOutSeconds)); }

            }
        }

        public EOperationalStatus OperationalStatus { get; private set; }

        public bool Contains(string key)
        {
            return _cache.Contains(key);
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
            lock (OLock) { _cache.Remove(key); }
        }

        public void SetSingleton(object value, string fullName = null)
        {
            var n = (fullName ?? value.GetType().FullName) + ":s";
            this[n] = value.ToJson();
        }

        public void RemoveAll(string oSet = null)
        {
            _cache.Dispose();
            _cache = new MemoryCache("NyanCache");
        }
    }
}
