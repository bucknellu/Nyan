using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Data.Common;

namespace Nyan.Modules.Data.Oracle
{
    public class OracleAdapter : AdapterPrimitive
    {
        public override string ParameterIdentifier
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool UseOutputParameterForInsertedKeyExtraction
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override void CheckDatabaseEntities<T>()
        {
            throw new NotImplementedException();
        }

        public override void ClearPools()
        {
            throw new NotImplementedException();
        }

        public override DbConnection Connection(string connectionString)
        {
            throw new NotImplementedException();
        }

        public override DynamicParametersPrimitive InsertableParameters<T>(object obj)
        {
            throw new NotImplementedException();
        }

        public override DynamicParametersPrimitive Parameters<T>(object obj)
        {
            throw new NotImplementedException();
        }

        public override void RenderSchemaEntityNames<T>()
        {
            throw new NotImplementedException();
        }

        public override void SetConnectionString<T>()
        {
            throw new NotImplementedException();
        }

        public override void SetSqlStatements<T>()
        {
            throw new NotImplementedException();
        }
    }
}
