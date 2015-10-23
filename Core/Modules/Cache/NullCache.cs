using System;
using System.Collections.Generic;

namespace Nyan.Core.Modules.Cache
{
    public class NullCacheProvider : ICacheProvider
    {
        public NullCacheProvider()
        {
            OperationalStatus = EOperationalStatus.NonOperational;
        }

        public string this[string key, string oSet, int cacheTimeOutSeconds]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public EOperationalStatus OperationalStatus { get; set; }

        public IEnumerable<string> GetAll(string oNamespace)
        {
            throw new NotImplementedException();
        }

        public bool Contains(string key)
        {
            if (OperationalStatus == EOperationalStatus.NonOperational) return false;
            throw new NotImplementedException();
        }

        public void Remove(string key, string oSet = null)
        {
            // Commenting to make Sample Program work.
            // throw new NotImplementedException();
        }

        public void SetSingleton(object value, string fullName = null)
        {
            throw new NotImplementedException();
        }

        T ICacheProvider.GetSingleton<T>(string fullName)
        {
            throw new NotImplementedException();
        }

        public void RemoveAll(string oSet = null)
        {
            throw new NotImplementedException();
        }

        public object GetSingleton<T>(string fullName = null)
        {
            throw new NotImplementedException();
        }
    }
}