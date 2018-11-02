using System.Collections.Generic;
using Nyan.Core.Modules.Data.Connection;

namespace Nyan.Core.Modules.Data
{
    public class InterceptorQuery
    {
        public enum EType
        {
            StaticArray
        }

        public enum EOperation
        {
            Query,
            Distinct,
            Update
        }
    }



    public interface IInterceptor
    {
        void Connect<T>(string statementsConnectionString, ConnectionBundlePrimitive bundle) where T : MicroEntity<T>;
        T Get<T>(string locator) where T : MicroEntity<T>;
        string Save<T>(MicroEntity<T> microEntity) where T : MicroEntity<T>;
        void Remove<T>(string locator) where T : MicroEntity<T>;
        void BulkSave<T>(List<T> source) where T : MicroEntity<T>;
        void Remove<T>(MicroEntity<T> microEntity) where T : MicroEntity<T>;
        void RemoveAll<T>() where T : MicroEntity<T>;
        void Insert<T>(MicroEntity<T> microEntity) where T : MicroEntity<T>;
        List<T> Do<T>(InterceptorQuery.EOperation pOperation, object query, object parm = null);
        List<T> Query<T>(string sqlStatement, object rawObject) where T : MicroEntity<T>;
        List<TU> Query<T, TU>(string statement, object rawObject, InterceptorQuery.EType ptype) where T : MicroEntity<T>;
        List<TU> Query<T, TU>(string statement, object rawObject, InterceptorQuery.EType ptype, InterceptorQuery.EOperation pOperation) where T : MicroEntity<T>;
        List<T> GetAll<T>(string extraParms = null) where T : MicroEntity<T>;
        List<TU> GetAll<T, TU>(string extraParms = null) where T : MicroEntity<T>;
        List<T> GetAll<T>(MicroEntityParametrizedGet parm, string extraParms = null) where T : MicroEntity<T>;
        List<TU> GetAll<T, TU>(MicroEntityParametrizedGet parm, string extraParms = null) where T : MicroEntity<T>;
        void Initialize<T>() where T : MicroEntity<T>;
        long RecordCount<T>() where T : MicroEntity<T>;
        long RecordCount<T>(string extraParms) where T : MicroEntity<T>;
        long RecordCount<T>(MicroEntityParametrizedGet qTerm) where T : MicroEntity<T>;
        long RecordCount<T>(MicroEntityParametrizedGet qTerm, string extraParms) where T : MicroEntity<T>;
        List<T> ReferenceQueryByField<T>(string field, string id) where T : MicroEntity<T>;
        List<T> ReferenceQueryByField<T>(object query) where T : MicroEntity<T>;
        void Setup<T>(MicroEntityCompiledStatements statements) where T : MicroEntity<T>;
        List<T> Get<T>(List<string> identifiers);
    }
}