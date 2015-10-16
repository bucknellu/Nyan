using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Cache
{
    public class NullCache : ICacheProvider
    {
        public NullCache()
        {
            OperationalStatus = EOperationalStatus.NonOperational;
        }

        public string this[string key, string oSet, int cacheTimeOutSeconds]
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public EOperationalStatus OperationalStatus { get; set; }

        public IEnumerable<string> GetAll(string oNamespace)
        {
            throw new System.NotImplementedException();
        }

        public bool Contains(string key)
        {
            if (OperationalStatus == EOperationalStatus.NonOperational) return false;
            throw new System.NotImplementedException();
        }

        public void Remove(string key, string oSet = null)
        {
            throw new System.NotImplementedException();
        }

        public void SetSingleton(object value, string fullName = null)
        {
            throw new System.NotImplementedException();
        }

        T ICacheProvider.GetSingleton<T>(string fullName)
        {
            throw new System.NotImplementedException();
        }

        public object GetSingleton<T>(string fullName = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
