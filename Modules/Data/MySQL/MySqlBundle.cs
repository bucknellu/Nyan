using System.Collections.Generic;
using Nyan.Core.Modules.Data.Connection;

namespace Nyan.Modules.Data.MySql
{
    public class MySqlBundle : ConnectionBundlePrimitive
    {
        private string _dbName;

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

            AdapterType = typeof (MySqlDataAdapter);
            ConnectionCypherKeys = new Dictionary<string, string>
            {
                {"STD", "server=localhost;user id=root;persistsecurityinfo=True;database=nyan;password=123"}
            };
        }
    }
}