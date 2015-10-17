using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Data.Common;

namespace Nyan.Modules.Data.Oracle
{
    public class OracleAdapter : AdapterPrimitive
    {
        public override void CheckDatabaseEntities<T>()
        {
            throw new NotImplementedException();
        }

        public override DbConnection Connection(string connectionString)
        {
            throw new NotImplementedException();
        }

        public override void RenderSchemaEntityNames<T>()
        {
            throw new NotImplementedException();
        }
    }
}