using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Cache
{
    public enum EOperationalStatus
    {
        Undefined,
        Initialized,
        Operational,
        Error,
        Recovering,
        NonOperational
    }

    public interface ICacheProvider
    {
        string this[string key, string oSet = null, int cacheTimeOutSeconds = 600]
        // Standard cache timeout: 10m (600 secs)
        { get; set; }

        EOperationalStatus OperationalStatus { get;}

        IEnumerable<string> GetAll(string oNamespace);
        bool Contains(string key);
        void Remove(string key, string oSet = null);
        void SetSingleton(object value, string fullName = null);
        T GetSingleton<T>(string fullName = null);
    }
}
