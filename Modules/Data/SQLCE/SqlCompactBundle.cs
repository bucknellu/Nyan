using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
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
                {"STD", "Data Source='" + Current.BaseDirectory + "\\" + _dbName + "';"}
            };
        }

        public override void ValidateDatabase()
        {
            if (File.Exists(Current.BaseDirectory + "\\" + _dbName)) return;

            var connString = "Data Source='" + Current.BaseDirectory + "\\" + _dbName + "';";
            var engine = new SqlCeEngine(connString);
            engine.CreateDatabase();
        }
    }
}