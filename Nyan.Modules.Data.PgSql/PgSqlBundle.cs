using Nyan.Core.Modules.Data.Connection;
using System.Collections.Generic;

namespace Nyan.Modules.Data.PgSql
{
    public class PgSqlBundle : BundlePrimitive
    {
        private string _dbName = null;

        public PgSqlBundle()
        {
            Initialize();
        }

        public PgSqlBundle(string dbName)
        {
            Initialize(dbName);
        }

        private void Initialize(string dbName = "Nyan.PgSql.db")
        {
            _dbName = dbName;

            AdapterType = typeof(PgSqlDataAdapter);
            EnvironmentCypherKeys = new Dictionary<string, string> { { "STD", "Database=nyan;User ID=postgres;Host=localhost;Password=123;Port=5432" } };
        }

        public override void ValidateDatabase()
        {
            // throw new NotImplementedException();
        }
    }
}
