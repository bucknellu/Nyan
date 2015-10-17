using Nyan.Core.Modules.Data.Connection;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Nyan.Portable.Modules.Data.Connection
{
    public class SQLiteBundle : BundlePrimitive
    {
        public SQLiteBundle()
        {
            AdapterType = typeof(Nyan.Modules.Data.SQLite.SQLiteDataAdapter);
            EnvironmentCypherKeys = new Dictionary<string, string> { { "STD", "Data Source=Nyan.sqlite.db;Version=3;" } };
        }

        public override void ValidateDatabase()
        {
            if (!File.Exists("Nyan.sqlite.db"))
            {
                SQLiteConnection.CreateFile("Nyan.sqlite.db");
            }
        }
    }
}
