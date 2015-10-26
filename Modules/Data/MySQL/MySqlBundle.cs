using Nyan.Core.Modules.Data.Connection;
using System.Collections.Generic;
using Nyan.Modules.Data.MySql;

namespace Nyan.Modules.Data.MySQL
{
    public class MySqlBundle : BundlePrimitive
    {
        private string _dbName = null;

        public MySqlBundle()
        {
            Initialize();
        }

        public MySqlBundle(string dbName)
        {
            Initialize(dbName);
        }

        private void Initialize(string dbName = "Nyan.MySql.db")
        {
            _dbName = dbName;

            AdapterType = typeof(MySqlDataAdapter);
            EnvironmentCypherKeys = new Dictionary<string, string> { { "STD", "server=localhost;user id=root;persistsecurityinfo=True;database=nyan;password=123" } };
        }
    }
}