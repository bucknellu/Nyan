using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Nyan.Core.Modules.Data;
using Nyan.Core.Settings;

namespace Nyan.Modules.Data.MongoDB
{
    public class MongoDbinterceptor : IInterceptor
    {
        private static IMongoClient _client;
        private static IMongoDatabase _database;
        private IMongoCollection<BsonDocument> _collection;
        private string _sourceCollection;
        private MicroEntityCompiledStatements _statements;

        public MongoDbinterceptor(MongoDbAdapter mongoDbAdapter)
        {
            AdapterInstance = mongoDbAdapter;
            _client = new MongoClient();
        }

        public MongoDbAdapter AdapterInstance { get; set; }

        public void Connect<T>(string statementsConnectionString) where T : MicroEntity<T>
        {
            _database = _client.GetDatabase(statementsConnectionString);
            _statements = MicroEntity<T>.Statements;
            _sourceCollection = MicroEntity<T>.TableData.TableName;
            _collection = _database.GetCollection<BsonDocument>(_sourceCollection);
        }

        public List<T> Get<T>() where T : MicroEntity<T>
        {
            try
            {
                var collection = _database.GetCollection<BsonDocument>(_sourceCollection);

                var col = collection.Find(new BsonDocument()).ToListAsync();
                col.Wait();

                var res = col.Result.Select(v => BsonSerializer.Deserialize<T>(v)).ToList();
                return res;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }

        public T Get<T>(string locator) where T : MicroEntity<T>
        {
            var filter = Builders<BsonDocument>.Filter.Eq(_statements.IdProperty, locator);
            var col = _collection.Find(filter).ToListAsync();
            col.Wait();

            var target = col.Result.FirstOrDefault();
            return BsonSerializer.Deserialize<T>(target);
        }

        public string Save<T>(MicroEntity<T> obj) where T : MicroEntity<T>
        {
            if (obj.GetEntityIdentifier() == "") obj.SetEntityIdentifier(Guid.NewGuid().ToString());

            var id = obj.GetEntityIdentifier();

            var document = BsonSerializer.Deserialize<BsonDocument>(obj.ToJson());
            _collection.InsertOne(document);

            return id;
        }

        public void Remove<T>(string locator) where T : MicroEntity<T>
        {
            var filter = Builders<BsonDocument>.Filter.Eq(_statements.IdProperty, locator);
            _collection.DeleteOne(filter);
        }

        public void Remove<T>(MicroEntity<T> microEntity) where T : MicroEntity<T>
        {
            Remove<T>(microEntity.GetEntityIdentifier());
        }

        public void RemoveAll<T>() where T : MicroEntity<T>
        {
            _collection.DeleteMany(FilterDefinition<BsonDocument>.Empty);
        }

        public void Insert<T>(MicroEntity<T> microEntity) where T : MicroEntity<T> { Save(microEntity); }
    }
}