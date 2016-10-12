using Nyan.Core.Modules.Data.Connection;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Nyan.Modules.Data.SQLite
{
    public class SqLiteBundle : ConnectionBundlePrimitive
    {
        private string _dbName;

        public SqLiteBundle()
        {
            Initialize();
        }

        public SqLiteBundle(string dbName)
        {
            Initialize(dbName);
        }

        private void Initialize(string dbName = "Nyan.sqlite.db")
        {
            _dbName = dbName;

            AdapterType = typeof(SqLiteDataAdapter);
            ConnectionCypherKeys = new Dictionary<string, string> { { "STA", "Data Source=" + Core.Configuration.DataDirectory + "\\" + _dbName + ";Version=3;" } };
        }

        public override void ValidateDatabase()
        {
            if (!File.Exists(Core.Configuration.DataDirectory + "\\" + _dbName))
            {
                SQLiteConnection.CreateFile(Core.Configuration.DataDirectory + "\\" + _dbName);
            }
        }
    }
}
