using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Data.Connection;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Data.MongoDB
{
    public class MongoDbinterceptor : IInterceptor
    {
        private static IMongoClient _client;
        private static IMongoDatabase _database;

        private static readonly List<Type> _typeCache = new List<Type>();
        private IMongoCollection<BsonDocument> _collection;

        private string _idProp;
        private object _instance;
        private Type _refType;
        private string _sourceCollection;
        private MicroEntityCompiledStatements _statements;
        private MicroEntitySetupAttribute _tabledata;

        public MongoDbinterceptor(MongoDbAdapter mongoDbAdapter) { AdapterInstance = mongoDbAdapter; }

        public MongoDbAdapter AdapterInstance { get; set; }

        private string Identifier
        {
            get
            {
                if (_idProp != null) return _idProp;

                _idProp = _statements.IdPropertyRaw;
                // if (_idProp.ToLower() == "id") _idProp = "_id";
                return _idProp;
            }
        }

        public void Connect<T>(string statementsConnectionString, ConnectionBundlePrimitive bundle)
            where T : MicroEntity<T>
        {
            _refType = typeof(T);

            // There's probably a better way (I hope) but for now...
            try { BsonSerializer.RegisterSerializer(typeof(DateTime), DateTimeSerializer.LocalInstance); } catch { }
            try { BsonTypeMapper.RegisterCustomTypeMapper(typeof(JObject), new JObjectMapper()); } catch { }

            _client = new MongoClient(statementsConnectionString);
            var dbname = MongoUrl.Create(statementsConnectionString).DatabaseName;

            try
            {
                dynamic resolverRef = bundle;
                dbname = resolverRef.GetDatabaseName();
            }
            catch (Exception e)
            {
                Current.Log.Add("MongoDbinterceptor: Failed to resolve database - " + e.Message,
                                Message.EContentType.Warning);
                dbname = "storage";
            }

            // https://jira.mongodb.org/browse/CSHARP-965
            // http://stackoverflow.com/questions/19521626/mongodb-convention-packs

            var pack = new ConventionPack { new IgnoreExtraElementsConvention(true) };

            ConventionRegistry.Register("ignore extra elements", pack, t => true);
            ConventionRegistry.Register("DictionaryRepresentationConvention", new ConventionPack { new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays) }, _ => true);
            ConventionRegistry.Register("EnumStringConvention", new ConventionPack { new EnumRepresentationConvention(BsonType.String) }, t => true);

            _database = _client.GetDatabase(dbname);
            _statements = MicroEntity<T>.Statements;
            _tabledata = MicroEntity<T>.TableData;

            RegisterGenericChain(typeof(T));

            SetSourceSollection();
        }

        public T Get<T>(string locator) where T : MicroEntity<T>
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", locator);
            var col = _collection.Find(filter).ToList();
            var target = col.FirstOrDefault();

            return target == null ? null : BsonSerializer.Deserialize<T>(target);
        }

        public string Save<T>(MicroEntity<T> obj) where T : MicroEntity<T>
        {
            try
            {
                if (obj.GetEntityIdentifier() == "") obj.SetEntityIdentifier(Guid.NewGuid().ToString());

                var id = obj.GetEntityIdentifier();

                var probe = Get<T>(id);

                var document = BsonSerializer.Deserialize<BsonDocument>(obj.ToJson());

                if (probe == null) { _collection.InsertOne(document); }
                else
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                    _collection.ReplaceOne(filter, document);
                }

                return id;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                Current.Log.Add(obj.ToJson(), Message.EContentType.Info);
                throw;
            }
        }

        public void Remove<T>(string locator) where T : MicroEntity<T>
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", locator);
            _collection.DeleteOne(filter);
        }

        public void Remove<T>(MicroEntity<T> microEntity) where T : MicroEntity<T> { Remove<T>(microEntity.GetEntityIdentifier()); }

        public void RemoveAll<T>() where T : MicroEntity<T> { _collection.DeleteMany(FilterDefinition<BsonDocument>.Empty); }

        public void Insert<T>(MicroEntity<T> obj) where T : MicroEntity<T>
        {
            if (obj.GetEntityIdentifier() == "") obj.SetEntityIdentifier(Guid.NewGuid().ToString());

            var document = BsonSerializer.Deserialize<BsonDocument>(obj.ToJson());
            _collection.InsertOne(document);
        }

        public List<T> Query<T>(string sqlStatement, object rawObject) where T : MicroEntity<T>
        {
            var rawQuery = sqlStatement ?? BsonExtensionMethods.ToJson(rawObject);
            Current.Log.Add($"{typeof(T).FullName}: QUERY {rawQuery}");
            var col = _collection.Find(BsonDocument.Parse(rawQuery)).ToEnumerable();
            var transform = col.AsParallel().Select(a => BsonSerializer.Deserialize<T>(a)).ToList();
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

            var res = colRes.Result.AsParallel().Select(v => BsonSerializer.Deserialize<T>(v)).ToList();
            return res;
        }

        public long RecordCount<T>() where T : MicroEntity<T> { return _collection.Count(new BsonDocument()); }

        public long RecordCount<T>(MicroEntityParametrizedGet qTerm) where T : MicroEntity<T> { return Get<T>(qTerm).Count; }

        public List<T> ReferenceQueryByField<T>(string field, string id) where T : MicroEntity<T>
        {
            var q = $"{{'{field}': '{id}'}}";
            return Query<T>(q, null);
        }

        public List<T> ReferenceQueryByField<T>(object query) where T : MicroEntity<T>
        {
            var q = query
                .ToDictionary()
                .ToJson();

            return Query<T>(q, null);
        }

        public List<TU> Query<TU>(string statement, object rawObject, InterceptorQuery.EType ptype)
        {
            List<TU> ret = null;

            switch (ptype)
            {
                case InterceptorQuery.EType.StaticArray:

                    switch (statement)
                    {
                        case "distinct":

                            var parm = rawObject.ToString();

                            ret = _collection.Distinct<TU>(parm, new BsonDocument()).ToList();
                            break;
                        default: break;
                    }
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(ptype), ptype, null);
            }

            return ret;
        }

        public void Initialize<T>() where T : MicroEntity<T>
        {
            // Check for the presence of text indexes '$**'
            try { _collection.Indexes.CreateOne(Builders<BsonDocument>.IndexKeys.Text("$**")); }
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

        private void RegisterGenericChain(Type type)
        {
            try
            {
                while (type != null && type.BaseType != null)
                {
                    if (!_typeCache.Contains(type))
                    {
                        _typeCache.Add(type);

                        if (!type.IsAbstract)
                        if (!type.IsGenericType)
                        {
                            if (!BsonClassMap.IsClassMapRegistered(type))
                            {
                                Current.Log.Add("MongoDbinterceptor: Registering " + type.FullName);

                                var classMapDefinition = typeof(BsonClassMap<>);
                                var classMapType = classMapDefinition.MakeGenericType(type);
                                var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);

                                // Do custom initialization here, e.g. classMap.SetDiscriminator, AutoMap etc

                                classMap.AutoMap();
                                classMap.MapIdProperty(Identifier);

                                BsonClassMap.RegisterClassMap(classMap);
                            }
                        }
                        else { foreach (var t in type.GetTypeInfo().GenericTypeArguments) RegisterGenericChain(t); }
                    }

                    type = type.BaseType;
                }
            }
            catch (Exception e) { Current.Log.Add(e, "Error registering class " + type.Name + ": " + e.Message); }
        }

        private void ClassMapInitializer(BsonClassMap<MongoDbinterceptor> cm)
        {
            cm.AutoMap();
            cm.MapIdMember(c => c.Identifier);
        }

        private void SetSourceSollection()
        {
            string s;

            if (typeof(IMongoDbCollectionResolver).IsAssignableFrom(_refType))
            {
                _instance = _refType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                s = ((IMongoDbCollectionResolver)_instance).GetCollectionName();
            }
            else
            {
                s = string.IsNullOrEmpty(_tabledata.TableName)
                    ? _refType.FullName
                    : _tabledata.TableName;
            }

            _sourceCollection = _statements.EnvironmentCode + "." + s;
            SetCollection();
        }

        private void SetCollection() { _collection = _database.GetCollection<BsonDocument>(_sourceCollection); }

        public void AddSourceCollectionSuffix(string suffix)
        {
            _sourceCollection += "." + suffix;

            SetCollection();
        }
    }
}