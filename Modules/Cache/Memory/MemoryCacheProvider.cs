using Nyan.Core.Extensions;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Shared;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace Nyan.Modules.Cache.Memory
{
    [Priority(Level = -1)]
    public class MemoryCacheProvider : ICacheProvider
    {
        private MemoryCache _cache;

        public MemoryCacheProvider()
        {
            OperationalStatus = EOperationalStatus.Initialized;
            //_cache = new MemoryCache("NyanCache");
            _cache = MemoryCache.Default;

            OperationalStatus = EOperationalStatus.Operational;
        }

        static readonly object OLock = new object();

        public string this[string key, string oSet = null, int cacheTimeOutSeconds = 600]
        {
            get
            {
                if (OperationalStatus != EOperationalStatus.Operational) return null;
                var res = _cache.Get(key);
                return res == null ? null : res.ToString();
            }

            set
            {
                if (OperationalStatus != EOperationalStatus.Operational) return;

                if (value == null)
                    Remove(key);
                else
                    lock (OLock) {
                        CacheItemPolicy policy = new CacheItemPolicy();
                        policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cacheTimeOutSeconds);
                        _cache.Set(key, value, policy);
                    }
            }
        }

        public Dictionary<string, ICacheConfiguration> EnvironmentConfiguration { get; set; }

        public string ServerName { get; private set; }
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

        public void Initialize()
        {
            //Not really necessary for memory cache.
        }

        public void Shutdown() { }

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
