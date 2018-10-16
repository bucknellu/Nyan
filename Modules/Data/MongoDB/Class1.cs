using System.Collections.Generic;
using MongoDB.Driver;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Data.MongoDB
{
    public static class Instances
    {
        public static Dictionary<string, MongoClient> Clients = new Dictionary<string, MongoClient>();

        private static readonly object LockObj = new object();

        public static MongoClient GetClient(string connectionString)
        {
            lock (LockObj)
            {
                var key = Current.Encryption.Encrypt(connectionString);

                if (Clients.ContainsKey(key)) return Clients[key];

                var client = new MongoClient(connectionString);

                Clients.Add(key, client);

                Current.Log.Add($"Nyan.Modules.Data.MongoDB.Clients: {Clients.Count} | Global", Message.EContentType.StartupSequence);

                return Clients[key];
            }
        }
    }
}