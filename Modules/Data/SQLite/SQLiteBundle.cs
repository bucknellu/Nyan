using Nyan.Core.Modules.Data.Connection;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Nyan.Modules.Data.SQLite
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

            AdapterType = typeof(SQLiteDataAdapter);
            EnvironmentCypherKeys = new Dictionary<string, string> { { "STD", "Data Source=" + Core.Settings.Current.BaseDirectory + "\\" + _dbName + ";Version=3;" } };
        }

        public override void ValidateDatabase()
        {
            if (!File.Exists(Core.Settings.Current.BaseDirectory + "\\" + _dbName))
            {
                SQLiteConnection.CreateFile(Core.Settings.Current.BaseDirectory + "\\" + _dbName);
            }
        }
    }
}
