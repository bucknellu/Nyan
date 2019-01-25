using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Data.MongoDB
{
    public static class Instances
    {
        public static ConcurrentDictionary<string, MongoClient> Clients = new ConcurrentDictionary<string, MongoClient>();

        private static readonly object LockObj = new object();

        public static MongoClient GetClient(string connectionString)
        {
            lock (LockObj)
            {
                var key = Current.Encryption.Encrypt(connectionString).Md5Hash();

                if (Clients.ContainsKey(key)) return Clients[key];

                var client = new MongoClient(connectionString);

                Clients[key] = client;

                string server;

                try { server = client.Settings.Servers.ToList()[0].Host; } catch (Exception) { server = client.Settings.Server.Host; }
                if (server != null) server = " @ " + server;

                Current.Log.Add($"MONGODB_CLIENT_REGISTER {client.Settings.Credential.Identity.Username}{server}", Message.EContentType.StartupSequence);

                return client;
            }
        }
    }
}