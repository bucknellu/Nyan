using Nyan.Core.Modules.Data.Connection;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Nyan.Portable.Modules.Data.Connection
{
    public class SQLiteBundle : BundlePrimitive
    {
        private string _dbName = null;


        public SQLiteBundle()
        {
            Initialize();
        }

        public SQLiteBundle(string dbName)
        {
            Initialize(dbName);
        }

        private void Initialize(string dbName = "Nyan.sqlite.db")
        {
            _dbName = dbName;

            AdapterType = typeof(Nyan.Modules.Data.SQLite.SQLiteDataAdapter);
            EnvironmentCypherKeys = new Dictionary<string, string> { { "STD", "Data Source=" + Nyan.Core.Settings.Current.BaseDirectory + "/" + _dbName + ";Version=3;" } };
        }

        public override void ValidateDatabase()
        {
            if (!File.Exists(Nyan.Core.Settings.Current.BaseDirectory + "/" + _dbName))
            {
                SQLiteConnection.CreateFile(Nyan.Core.Settings.Current.BaseDirectory + "/" + _dbName);
            }
        }
    }
}
