using System;
using System.Collections.Generic;
using System.Linq;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Core.Shared;
using StackExchange.Redis;

namespace Nyan.Modules.Cache.Redis
{
    [Priority(Level = -1)]
    public class RedisCacheProvider : ICacheProvider
    {
        private static ConnectionMultiplexer _redis;

        private static string _currentServer = "none";
        public Dictionary<string, ICacheConfiguration> EnvironmentConfiguration { get; set; }

        public string this[string key, string oSet = null, int cacheTimeOutSeconds = 600]
        {
            get
            {
                if (OperationalStatus != EOperationalStatus.Operational) return null;

                try
                {
                    var db = _redis.GetDatabase(DatabaseIndex);
                    var res = db.StringGet(key);
                    return res;
                }
                catch (Exception e)
                {
                    Current.Log.Add(e);
                    return null;

                    //OperationalStatus = EOperationalStatus.Error;
                    //throw;
                }
            }
            set
            {
                if (OperationalStatus != EOperationalStatus.Operational) return;

                try
                {
                    var db = _redis.GetDatabase(DatabaseIndex);

                    if (cacheTimeOutSeconds == 0)
                        db.StringSet(key, value);
                    else
                        db.StringSet(key, value, TimeSpan.FromSeconds(cacheTimeOutSeconds));

                    //if (oSet != null)
                    //    db.SetAdd(oSet, value);
                }
                catch (Exception e)
                {
                    Current.Log.Add(e);

                    //OperationalStatus = EOperationalStatus.Error;
                    //throw;
                }
            }
        }

        public string ServerName { get; private set; }

        public EOperationalStatus OperationalStatus { get; set; }

        public bool Contains(string key)
        {
            if (OperationalStatus != EOperationalStatus.Operational) return false;

            try
            {
                var db = _redis.GetDatabase(DatabaseIndex);
                var res = db.KeyExists(key);
                return res;
            }
            catch (Exception)
            {
                OperationalStatus = EOperationalStatus.Error;
                throw;
            }
        }

        public IEnumerable<string> GetAll(string oNamespace)
        {
            if (OperationalStatus != EOperationalStatus.Operational) return null;

            try
            {
                var db = _redis.GetDatabase(DatabaseIndex);
                var conn = _redis.GetEndPoints()[0];
                var svr = _redis.GetServer(conn);
                var keys = svr.Keys(pattern: oNamespace + "*").ToList();

                var ret = keys.Select(a => db.StringGet(a).ToString()).ToList();
                return ret;

                //return db.SetMembers(oNamespace).Select(i => i.ToString()).ToList();
            }
            catch (Exception)
            {
                OperationalStatus = EOperationalStatus.Error;
                throw;
            }
        }

        public T GetSingleton<T>(string fullName = null)
        {
            var n = (fullName ?? typeof(T).FullName) + ":s";
            var c = this[n];

            return c == null ? default(T) : c.FromJson<T>();
        }

        public void Remove(string key, string oSet = null)
        {
            if (OperationalStatus != EOperationalStatus.Operational) return;

            try
            {
                var db = _redis.GetDatabase(DatabaseIndex);
                //if (oSet != null)
                //db.SetRemove(oSet, this[key]);

                db.KeyDelete(key);
            }
            catch { }
        }

        public void RemoveAll(string oSet = null)
        {
            if (OperationalStatus != EOperationalStatus.Operational) return;

            foreach (var endPoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endPoint);

                var db = _redis.GetDatabase(DatabaseIndex);

                var keys = server.Keys(pattern: "*", database: DatabaseIndex).ToList();

                Current.Log.Add("REDIS: Removing {0} keys from database {1}".format(keys.Count, DatabaseIndex),
                    Message.EContentType.Maintenance);

                foreach (var key in keys) db.KeyDelete(key);

                //server.FlushDatabase(DatabaseIndex);
            }
        }

        public void SetSingleton(object value, string fullName = null)
        {
            if (OperationalStatus != EOperationalStatus.Operational) return;

            var n = (fullName ?? value.GetType().FullName) + ":s";
            this[n] = value.ToJson();
        }

        #region custom implementation

        private static int DatabaseIndex { get; set; } = -1;

        public void Initialize()
        {
            //In the case nothing is defined, a standard environment setup is provided.
            if (EnvironmentConfiguration == null)
                EnvironmentConfiguration = new Dictionary<string, ICacheConfiguration>
                {
                    {"STA", new RedisCacheConfiguration {DatabaseIndex = 5, ConnectionString = "localhost"}}
                };

            var probe = (RedisCacheConfiguration)EnvironmentConfiguration[Current.Environment.CurrentCode];
            DatabaseIndex = probe.DatabaseIndex;
            _currentServer = probe.ConnectionString;

            Connect();
        }

        public void Shutdown() { }

        public void Connect()
        {
            try
            {
                ServerName = _currentServer.Split(',')[0];

                //The connection string may be encrypted. Try to decrypt it, but ignore if it fails.
                try
                {
                    _currentServer = Current.Encryption.Decrypt(_currentServer);
                }
                catch { }

                Current.Log.Add("Redis server      : Connecting to " + _currentServer.SafeArray("password"),
                    Message.EContentType.MoreInfo);

                _redis = ConnectionMultiplexer.Connect(_currentServer);
                OperationalStatus = EOperationalStatus.Operational;
            }
            catch (Exception e)
            {
                OperationalStatus = EOperationalStatus.NonOperational;
                Current.Log.Add("REDIS unavailable: Application running on direct database mode.",
                    Message.EContentType.Maintenance);
                Current.Log.Add(e);
            }
        }

        #endregion
    }
}