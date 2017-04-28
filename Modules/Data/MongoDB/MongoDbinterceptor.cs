using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Data.MongoDB
{
    public class MongoDbinterceptor : IInterceptor
    {
        private static IMongoClient _client;
        private static IMongoDatabase _database;
        private IMongoCollection<BsonDocument> _collection;

        private string _idProp;
        private string _sourceCollection;
        private MicroEntityCompiledStatements _statements;

        public MongoDbinterceptor(MongoDbAdapter mongoDbAdapter) { AdapterInstance = mongoDbAdapter; }

        public MongoDbAdapter AdapterInstance { get; set; }

        private string Identifier
        {
            get
            {
                if (_idProp != null) return _idProp;

                _idProp = _statements.IdPropertyRaw.ToLower();
                if (_idProp == "id") _idProp = "_id";

                return _idProp;
            }
        }

        public void Connect<T>(string statementsConnectionString) where T : MicroEntity<T>
        {
            _client = new MongoClient(statementsConnectionString);

            // https://jira.mongodb.org/browse/CSHARP-965
            // http://stackoverflow.com/questions/19521626/mongodb-convention-packs

            var pack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("ignore extra elements", pack, t => true);

            ConventionRegistry.Register("DictionaryRepresentationConvention", new ConventionPack { new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays) }, _ => true);

            _database = _client.GetDatabase("storage");
            _statements = MicroEntity<T>.Statements;

            if (MicroEntity<T>.TableData.TableName == "") _sourceCollection = _statements.EnvironmentCode + "." + typeof(T).FullName;

            _sourceCollection = _statements.EnvironmentCode + "."
                                + (string.IsNullOrEmpty(MicroEntity<T>.TableData.TableName)
                                    ? typeof(T).FullName
                                    : MicroEntity<T>.TableData.TableName);

            _collection = _database.GetCollection<BsonDocument>(_sourceCollection);
        }

        public T Get<T>(string locator) where T : MicroEntity<T>
        {
            var filter = Builders<BsonDocument>.Filter.Eq(Identifier, locator);
            var col = _collection.Find(filter).ToListAsync();
            col.Wait();

            var target = col.Result.FirstOrDefault();

            return target == null ? null : BsonSerializer.Deserialize<T>(target);
        }

        public string Save<T>(MicroEntity<T> obj) where T : MicroEntity<T>
        {
            if (obj.GetEntityIdentifier() == "") obj.SetEntityIdentifier(Guid.NewGuid().ToString());

            var id = obj.GetEntityIdentifier();

            var probe = Get<T>(id);

            var document = BsonSerializer.Deserialize<BsonDocument>(obj.ToJson());

            if (probe == null)
            {
                _collection.InsertOne(document);
            }
            else
            {
                var filter = Builders<BsonDocument>.Filter.Eq(Identifier, id);
                _collection.ReplaceOne(filter, document);
            }

            return id;
        }

        public void Remove<T>(string locator) where T : MicroEntity<T>
        {
            var filter = Builders<BsonDocument>.Filter.Eq(Identifier, locator);
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

        public void Insert<T>(MicroEntity<T> obj) where T : MicroEntity<T>
        {
            if (obj.GetEntityIdentifier() == "") obj.SetEntityIdentifier(Guid.NewGuid().ToString());

            var document = BsonSerializer.Deserialize<BsonDocument>(obj.ToJson());
            _collection.InsertOne(document);
        }

        public List<T> Query<T>(string sqlStatement, object rawObject) where T : MicroEntity<T>
        {
            var rawQuery = sqlStatement ?? rawObject.ToJson();

            Current.Log.Add(typeof(T).FullName + ": QUERY " + rawQuery);

            var col = _collection.Find(BsonDocument.Parse(rawQuery)).ToListAsync();
            col.Wait();

            var transform = col.Result.Select(a => BsonSerializer.Deserialize<T>(a)).ToList();
            return transform;
        }

        public List<T> Get<T>(MicroEntityParametrizedGet parm) where T : MicroEntity<T>
        {
            List<T> ret = null;

            SortDefinition<BsonDocument> sortFilter = new BsonDocument();

            var queryFilter = (parm.QueryTerm ?? "") != ""
                ? new BsonDocument { { "$text", new BsonDocument { { "$search", parm.QueryTerm } } } }
                : new BsonDocument();

            if (parm.OrderBy != null)
            {
                var sign = parm.OrderBy[0];
                var deSignedValue = parm.OrderBy.Substring(1);

                int dir;
                string field;

                switch (sign)
                {
                    case '+':
                        field = deSignedValue;
                        dir = +1;
                        break;
                    case '-':
                        field = deSignedValue;
                        dir = -1;
                        break;
                    default:
                        field = parm.OrderBy;
                        dir = +1;
                        break;
                }

                sortFilter = new BsonDocument(field, dir);
            }

            var col = _collection
                .Find(queryFilter)
                .Sort(sortFilter);

            if (parm.PageSize != 0)
            {
                var pos = (int)(parm.PageIndex * parm.PageSize);
                col = col.Skip(pos).Limit((int)parm.PageSize);
            }

            var colRes = col.ToListAsync();

            Task.WhenAll(colRes);

            var res = colRes.Result.Select(v => BsonSerializer.Deserialize<T>(v)).ToList();
            return res;
        }

        public long RecordCount<T>() where T : MicroEntity<T> { return _collection.Count(new BsonDocument()); }

        public long RecordCount<T>(MicroEntityParametrizedGet qTerm) where T : MicroEntity<T>
        {
            return Get<T>(qTerm).Count;
        }

        public void Initialize<T>() where T : MicroEntity<T>
        {
            // Check for the presence of text indexes '$**'
            try
            {
                _collection.Indexes.CreateOne(Builders<BsonDocument>.IndexKeys.Text("$**"));
            }
            catch (Exception e)
            {
                Current.Log.Add("ERR Creating index " + _sourceCollection + ": " + e.Message,
                    Message.EContentType.Warning);
            }
        }

        public List<T> Get<T>() where T : MicroEntity<T>
        {
            try
            {
                var res = _collection
                    .Find(new BsonDocument())
                    .ToList()
                    .Select(v => BsonSerializer.Deserialize<T>(v))
                    .ToList();

                return res;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }
    }
}