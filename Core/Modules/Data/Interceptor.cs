using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Data
{
    public interface IInterceptor
    {
        void Connect<T>(string statementsConnectionString) where T : MicroEntity<T>;
        List<T> Get<T>() where T : MicroEntity<T>;
        T Get<T>(string locator) where T : MicroEntity<T>;
        string Save<T>(MicroEntity<T> microEntity) where T : MicroEntity<T>;
        void Remove<T>(string locator) where T : MicroEntity<T>;
        void Remove<T>(MicroEntity<T> microEntity) where T : MicroEntity<T>;
        void RemoveAll<T>() where T : MicroEntity<T>;
        void Insert<T>(MicroEntity<T> microEntity) where T : MicroEntity<T>;
    }
}
