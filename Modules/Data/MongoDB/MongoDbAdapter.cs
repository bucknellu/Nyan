using System.Data.Common;
using Nyan.Core.Modules.Data.Adapter;

namespace Nyan.Modules.Data.MongoDB
{
    public class MongoDbAdapter : DataAdapterPrimitive
    {
        public MongoDbAdapter()
        {
            useOutputParameterForInsertedKeyExtraction = true;
            dynamicParameterType = typeof(MongoDbDynamicParameters);
            Interceptor = new MongoDbinterceptor(this);
        }

        public override void RenderSchemaEntityNames<T>() { }
        public override DbConnection Connection(string connectionString) { return null; }
        public override void CheckDatabaseEntities<T>() { }
    }
}