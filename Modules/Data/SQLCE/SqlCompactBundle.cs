using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
using Nyan.Core;
using Nyan.Core.Modules.Data.Connection;
using Nyan.Core.Settings;

namespace Nyan.Modules.Data.SQLCompact
{
    public class SqlCompactBundle : ConnectionBundlePrimitive
    {
        private string _dbName;

        public SqlCompactBundle()
        {
            Initialize();
        }

        public SqlCompactBundle(string dbName)
        {
            Initialize(dbName);
        }

        private void Initialize(string dbName = "nyanSqlCompactStorage.sdf")
        {
            _dbName = dbName;

            AdapterType = typeof (SqlCompactDataAdapter);
            ConnectionCypherKeys = new Dictionary<string, string>
            {
                {"STA", "Data Source='" + Configuration.DataDirectory + "\\" + _dbName + "';"}
            };
        }

        public override void ValidateDatabase()
        {
            if (File.Exists(Configuration.DataDirectory + "\\" + _dbName)) return;

            var connString = "Data Source='" + Configuration.DataDirectory + "\\" + _dbName + "';";
            var engine = new SqlCeEngine(connString);
            engine.CreateDatabase();
        }
    }
}