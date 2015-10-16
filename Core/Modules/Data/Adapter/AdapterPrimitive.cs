using System.Data.Common;

namespace Nyan.Core.Modules.Data.Adapter
{
    public abstract class AdapterPrimitive
    {
        public abstract void CheckDatabaseEntities<T>() where T : MicroEntity<T>;
        public abstract void SetSqlStatements<T>() where T : MicroEntity<T>;
        public abstract void SetConnectionString<T>() where T : MicroEntity<T>;
        public abstract void RenderSchemaEntityNames<T>() where T : MicroEntity<T>;
        public abstract void ClearPools();
        public abstract DynamicParametersPrimitive Parameters<T>(object obj) where T : MicroEntity<T>;
        public abstract DbConnection Connection(string connectionString);
        public abstract DynamicParametersPrimitive InsertableParameters<T>(object obj) where T : MicroEntity<T>;
        public abstract bool UseOutputParameterForInsertedKeyExtraction { get; }
        public abstract string ParameterIdentifier { get; }
    }
}
