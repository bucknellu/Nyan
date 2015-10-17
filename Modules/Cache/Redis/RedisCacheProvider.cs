using Nyan.Core.Modules.Cache;
using System;
using System.Collections.Generic;

namespace Nyan.Modules.Cache.Redis
{
    public class RedisCacheProvider : ICacheProvider
    {
        public string this[string key, string oSet = null, int cacheTimeOutSeconds = 600]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public EOperationalStatus OperationalStatus
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Contains(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetAll(string oNamespace)
        {
            throw new NotImplementedException();
        }

        public T GetSingleton<T>(string fullName = null)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key, string oSet = null)
        {
            throw new NotImplementedException();
        }

        public void RemoveAll(string oSet = null)
        {
            throw new NotImplementedException();
        }

        public void SetSingleton(object value, string fullName = null)
        {
            throw new NotImplementedException();
        }
    }
}
