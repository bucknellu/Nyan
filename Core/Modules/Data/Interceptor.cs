using System.Collections.Generic;

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
        List<T> Query<T>(string sqlStatement, object rawObject) where T : MicroEntity<T>;
        List<T> Get<T>(MicroEntityParametrizedGet parm) where T : MicroEntity<T>;
        void Initialize<T>() where T : MicroEntity<T>;
        long RecordCount<T>() where T : MicroEntity<T>;
        long RecordCount<T>(MicroEntityParametrizedGet qTerm) where T : MicroEntity<T>;
    }
}