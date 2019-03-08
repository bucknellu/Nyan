using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Nyan.Core.Diagnostics;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Data.Connection;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Data.MongoDB
{
    public class StaticLock<T> where T : MicroEntity<T>
    {
        public static object EntityLock { get; set; } = new object();
    }

    public class MongoDbinterceptor : IInterceptor
    {
        private IMongoClient _client;
        private static readonly List<Type> _typeCache = new List<Type>();

        private string _idProp;
        private object _instance;
        private Type _refType;
        private MicroEntityCompiledStatements _statements;
        private MicroEntitySetupAttribute _tabledata;
        public IMongoCollection<BsonDocument> Collection;
        public IMongoDatabase Database;
        public string SourceCollection;

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


        public void Connect<T>(string statementsConnectionString, ConnectionBundlePrimitive bundle) where T : MicroEntity<T>
        {
            _refType = typeof(T);

            // There's probably a better way (I hope) but for now...
            try { BsonSerializer.RegisterSerializer(typeof(DateTime), DateTimeSerializer.LocalInstance); } catch { }

            try { BsonSerializer.RegisterSerializer(typeof(JObject), new JObjectSerializer()); } catch { }

            try { BsonSerializer.RegisterSerializer(typeof(JValue), new JValueSerializer()); } catch { }

            try { BsonSerializer.RegisterSerializer(typeof(JArray), new JArraySerializer()); } catch { }

            try { BsonTypeMapper.RegisterCustomTypeMapper(typeof(JObject), new JObjectMapper()); } catch { }

            _client = Instances.GetClient(statementsConnectionString);
            var server = _client.Settings.Servers.ToList()[0].Host;

            // Current.Log.Add($"{typeof(T).FullName} client for {statementsConnectionString}: {_client?.Settings?.Credential?.Username}@{server}", Message.EContentType.StartupSequence);


            var dbname = MongoUrl.Create(statementsConnectionString).DatabaseName;

            try
            {
                dynamic resolverRef = bundle;
                dbname = resolverRef.GetDatabaseName(_statements?.EnvironmentCode);
            }
            catch (Exception e)
            {
                Current.Log.Add("MongoDbinterceptor: Failed to resolve database - " + e.Message, Message.EContentType.Warning);
                dbname = "storage";
            }

            // https://jira.mongodb.org/browse/CSHARP-965
            // http://stackoverflow.com/questions/19521626/mongodb-convention-packs

            var pack = new ConventionPack { new IgnoreExtraElementsConvention(true) };

            ConventionRegistry.Register("ignore extra elements", pack, t => true);
            ConventionRegistry.Register("DictionaryRepresentationConvention", new ConventionPack { new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays) }, _ => true);
            ConventionRegistry.Register("EnumStringConvention", new ConventionPack { new EnumRepresentationConvention(BsonType.String) }, t => true);

            Database = _client.GetDatabase(dbname);

            // Current.Log.Add($"{typeof(T).FullName} {Database.Client?.Settings?.Credential?.Username}:{Database?.DatabaseNamespace}@{server} - REGISTERING", Message.EContentType.StartupSequence);

            _statements = MicroEntity<T>.Statements;
            _tabledata = MicroEntity<T>.TableData;

            RegisterGenericChain(typeof(T));

            SetSourceCollection();

        }

        public T Get<T>(string locator) where T : MicroEntity<T>
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", locator);
                var col = Collection.Find(filter).ToList();
                var target = col.FirstOrDefault();

                if (target == null)
                {
                    var isNumeric = long.TryParse(locator, out var n);

                    if (isNumeric)
                    {
                        filter = Builders<BsonDocument>.Filter.Eq("_id", n);
                        col = Collection.Find(filter).ToList();
                        target = col.FirstOrDefault();
                    }
                }

                return target == null ? null : BsonSerializer.Deserialize<T>(target);
            }
            catch (Exception e)
            {
                Current.Log.Add($"{Database.Client.Settings.Credential.Username}@{Database.DatabaseNamespace} - {Collection.CollectionNamespace}:{locator} {e.Message}", Message.EContentType.Warning);
                throw;
            }
        }

        public string Save<T>(MicroEntity<T> obj) where T : MicroEntity<T>
        {
            string id = null;
            try
            {
                if (obj.GetEntityIdentifier() == "") obj.SetEntityIdentifier(Guid.NewGuid().ToString());

                id = obj.GetEntityIdentifier();

                lock (StaticLock<T>.EntityLock)
                {
                    var probe = Get<T>(id);

                    var document = BsonSerializer.Deserialize<BsonDocument>(obj.ToJson());

                    if (probe == null) { Collection.InsertOne(document); }
                    else
                    {
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                        Collection.ReplaceOne(filter, document);
                    }
                }

                return id;
            }
            catch (Exception e)
            {
                Current.Log.Add($"{ThreadHelper.Uid} {Database.Client.Settings.Credential.Username}@{Database.DatabaseNamespace} - {Collection.CollectionNamespace}:{id} {e.Message}", Message.EContentType.Warning);
                Current.Log.Add($"{ThreadHelper.Uid} {obj.ToJson()}", Message.EContentType.Warning);
                throw;
            }
        }

        public void Remove<T>(string locator) where T : MicroEntity<T>
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", locator);
            Collection.DeleteOne(filter);
        }

        public void BulkSave<T>(List<T> source) where T : MicroEntity<T>
        {
            var c = 0;

            if (source == null)
            {
                Current.Log.Add($"{ThreadHelper.Uid} - {Collection.CollectionNamespace}:BulkSave NULL list", Message.EContentType.Info);
                return;
            }

            if (source.Count == 0)
            {
                Current.Log.Add($"{ThreadHelper.Uid} - {Collection.CollectionNamespace}:BulkSave Empty list", Message.EContentType.Info);
                return;
            }

            T current = null;

            try
            {

                var buffer = new List<ReplaceOneModel<BsonDocument>>();



                foreach (var i in source)
                {

                    c++;

                    current = i;

                    buffer.Add(new ReplaceOneModel<BsonDocument>(
                                       Builders<BsonDocument>.Filter.Eq("_id", i.GetEntityIdentifier()),
                                       BsonSerializer.Deserialize<BsonDocument>(i.ToJson())
                                   )
                    { IsUpsert = true });
                }

                Collection.BulkWrite(buffer.ToArray());


            }
            catch (Exception e)
            {
                Current.Log.Add($"{ThreadHelper.Uid} - {Collection.CollectionNamespace}:BulkSave {c}/{source.Count} items ERR {e.Message}", Message.EContentType.Warning);
                Current.Log.Add($"{ThreadHelper.Uid} - {current.ToJson()}", Message.EContentType.Warning);
                Current.Log.Add(e);
                throw;
            }


        }

        public void Remove<T>(MicroEntity<T> microEntity) where T : MicroEntity<T> { Remove<T>(microEntity.GetEntityIdentifier()); }

        public void RemoveAll<T>() where T : MicroEntity<T> { Collection.DeleteMany(FilterDefinition<BsonDocument>.Empty); }

        public void Insert<T>(MicroEntity<T> obj) where T : MicroEntity<T>
        {
            if (obj.GetEntityIdentifier() == "") obj.SetEntityIdentifier(Guid.NewGuid().ToString());

            var document = BsonSerializer.Deserialize<BsonDocument>(obj.ToJson());
            Collection.InsertOne(document);
        }

        public List<T> Do<T>(InterceptorQuery.EOperation pOperation, object query, object parm = null)
        {
            switch (pOperation)
            {
                case InterceptorQuery.EOperation.Query: break;
                case InterceptorQuery.EOperation.Distinct: break;
                case InterceptorQuery.EOperation.Update:

                    if (parm == null) return null;
                    var bQuery = BsonDocument.Parse(Serialization.ToJson(query));
                    var bTrans = BsonDocument.Parse("{$set: " + Serialization.ToJson(parm) + " }");
                    Collection.UpdateMany(bQuery, bTrans);

                    return null;
                default: throw new ArgumentOutOfRangeException(nameof(pOperation), pOperation, null);
            }

            return null;
        }

        public List<T> Query<T>(string sqlStatement, object rawObject) where T : MicroEntity<T> { return Query<T, T>(sqlStatement, rawObject); }

        public List<TU> GetAll<T, TU>(string extraParms = null) where T : MicroEntity<T>
        {
            if (extraParms != null) return Query<T, TU>("{" + extraParms + "}", null);

            try { return GetAll<TU>(Collection); }
            catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }

        public List<T> GetAll<T>(MicroEntityParametrizedGet parm, string extraParms = null) where T : MicroEntity<T> { return GetAll<T, T>(parm, extraParms); }

        public List<TU> GetAll<T, TU>(MicroEntityParametrizedGet parm, string extraParms = null) where T : MicroEntity<T>
        {
            var queryFilter = parm.ToBsonQuery(extraParms);
            var querySort = parm.ToBsonFilter();
            SortDefinition<BsonDocument> sortFilter = querySort;

            if (parm.OrderBy != null) Collection.Indexes.CreateOne(querySort.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict }));

            IFindFluent<BsonDocument, BsonDocument> col;

            try
            {
                col = Collection
                    .Find(queryFilter)
                    .Sort(sortFilter);
            }
            catch (Exception e)
            {
                Current.Log.Add($"{ThreadHelper.Uid} {Database.Client.Settings.Credential.Username}@{Database.DatabaseNamespace} - {Collection.CollectionNamespace} {e.Message}", Message.EContentType.Warning);
                Current.Log.Add($"{ThreadHelper.Uid} {parm.ToJson()}", Message.EContentType.Warning);
                Current.Log.Add(e);
                throw;
            }

            if (parm.PageSize != 0)
            {
                var pos = (int)(parm.PageIndex * parm.PageSize);
                col = col.Skip(pos).Limit((int)parm.PageSize);
            }

            var colRes = col.ToListAsync();

            Task.WhenAll(colRes);

            var res = colRes.Result.AsParallel().Select(v => BsonSerializer.Deserialize<TU>(v)).ToList();
            return res;
        }

        public long RecordCount<T>() where T : MicroEntity<T> { return Collection.Count(new BsonDocument()); }

        public long RecordCount<T>(string extraParms) where T : MicroEntity<T> { return extraParms == null ? RecordCount<T>() : Collection.Count(BsonDocument.Parse(extraParms)); }

        public long RecordCount<T>(MicroEntityParametrizedGet qTerm) where T : MicroEntity<T> { return RecordCount<T>(qTerm, null); }

        public long RecordCount<T>(MicroEntityParametrizedGet qTerm, string parm) where T : MicroEntity<T>
        {
            var q = qTerm.ToBsonQuery(parm);
            return Collection.Count(q);
        }

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

        public void Setup<T>(MicroEntityCompiledStatements statements) where T : MicroEntity<T>
        {
            _statements = statements;
            Connect<T>(_statements.ConnectionString, _statements.Bundle);
        }

        public List<T> Get<T>(List<string> identifiers)
        {
            var filter = Builders<BsonDocument>.Filter.In("_id", identifiers);
            var col = Collection.Find(filter).ToList();
            var res = col.Select(i => BsonSerializer.Deserialize<T>(i)).ToList();
            return res;
        }

        public List<TU> Query<T, TU>(string statement, object rawObject, InterceptorQuery.EType ptype) where T : MicroEntity<T> { return Query<T, TU>(statement, rawObject, ptype, InterceptorQuery.EOperation.Query); }

        public List<TU> Query<T, TU>(string statement, object rawObject, InterceptorQuery.EType ptype, InterceptorQuery.EOperation pOperation) where T : MicroEntity<T>
        {
            List<TU> ret = null;

            switch (pOperation)
            {
                case InterceptorQuery.EOperation.Distinct:
                    var parm = rawObject.ToString();
                    ret = Collection.Distinct<TU>(parm, statement == null ? new BsonDocument() : BsonDocument.Parse(statement)).ToList();
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(ptype), ptype, null);
            }

            return ret;
        }

        public void Initialize<T>() where T : MicroEntity<T>
        {
            // Check for the presence of text indexes '$**'
            try { Collection.Indexes.CreateOne(Builders<BsonDocument>.IndexKeys.Text("$**")); }
            catch (Exception e)
            {
                string server;
                try { server = Database.Client.Settings.Servers.ToList()[0].Host; } catch (Exception) { server = Database.Client.Settings.Server.Host; }
                Current.Log.Add($"{ThreadHelper.Uid} {Database.Client?.Settings?.Credential?.Username}:{Database?.DatabaseNamespace}@{server} - {Collection?.CollectionNamespace} {e.Message} | ERR Creating index {SourceCollection}: {e.Message}", Message.EContentType.Warning);
            }
        }

        public List<T> GetAll<T>(string extraParms = null) where T : MicroEntity<T> { return GetAll<T, T>(); }

        public void CopyTo(string originSet, string destinationSet, bool wipeDestination = false)
        {
            var aggDoc = new Dictionary<string, object>
            {
                {"aggregate", originSet},
                {
                    "pipeline", new[]
                    {
                        new Dictionary<string, object> {{"$match", new BsonDocument()}},
                        new Dictionary<string, object> {{"$out", destinationSet}}
                    }
                },

                // https://stackoverflow.com/questions/47472688/spring-data-mongodb-the-cursor-option-is-required
                {"cursor", new Dictionary<string, object> {{ "batchSize", 1024 }}
            }
    };

            var doc = new BsonDocument(aggDoc);
            var command = new BsonDocumentCommand<BsonDocument>(doc);

            Database.RunCommand(command);
        }

        public List<TU> Query<T, TU>(string sqlStatement, object rawObject) where T : MicroEntity<T>
        {
            var rawQuery = sqlStatement ?? BsonExtensionMethods.ToJson(rawObject);

            // if (Current.Environment.CurrentCode == "DEV") Current.Log.Add($"{typeof(T).FullName}: QUERY {rawQuery}");

            var col = Collection.Find(BsonDocument.Parse(rawQuery)).ToEnumerable();
            var transform = col.AsParallel().Select(a => BsonSerializer.Deserialize<TU>(a)).ToList();

            return transform;
        }

        public List<TU> GetAll<TU>(IMongoCollection<BsonDocument> sourceCollection)
        {
            var res = sourceCollection
                .Find(new BsonDocument())
                .ToList()
                .Select(v => BsonSerializer.Deserialize<TU>(v))
                .ToList();

            return res;
        }

        public TU Get<TU>(IMongoCollection<BsonDocument> sourceCollection, string locator) where TU : MicroEntity<TU>
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", locator);
                var col = sourceCollection.Find(filter).ToList();
                var target = col.FirstOrDefault();

                if (target == null)
                {
                    var isNumeric = long.TryParse(locator, out var n);

                    if (isNumeric)
                    {
                        filter = Builders<BsonDocument>.Filter.Eq("_id", n);
                        col = sourceCollection.Find(filter).ToList();
                        target = col.FirstOrDefault();
                    }
                }

                return target == null ? null : BsonSerializer.Deserialize<TU>(target);
            }
            catch (Exception e)
            {
                Current.Log.Add($"{Database.Client.Settings.Credential.Username}@{Database.DatabaseNamespace} - {sourceCollection.CollectionNamespace}:{locator} {e.Message}", Message.EContentType.Warning);
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
                                    // Current.Log.Add("MongoDbinterceptor: Registering " + type.FullName);

                                    var classMapDefinition = typeof(BsonClassMap<>);
                                    var classMapType = classMapDefinition.MakeGenericType(type);
                                    var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);

                                    // Do custom initialization here, e.g. classMap.SetDiscriminator, AutoMap etc

                                    classMap.AutoMap();
                                    classMap.MapIdProperty(Identifier);

                                    BsonClassMap.RegisterClassMap(classMap);
                                }
                            }
                            else
                            {
                                foreach (var t in type.GetTypeInfo().GenericTypeArguments) RegisterGenericChain(t);
                            }
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

        private void SetSourceCollection()
        {
            string s;

            if (typeof(IMongoDbCollectionResolver).IsAssignableFrom(_refType))
            {
                _instance = _refType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                s = ((IMongoDbCollectionResolver)_instance).GetCollectionName();
                // Current.Log.Add("MongoDbinterceptor.SetSourceCollection: CUSTOM_INIT " + s, Message.EContentType.StartupSequence);
            }
            else
            {
                s = _refType.FullName;

                if (!string.IsNullOrEmpty(_tabledata.TablePrefix)) s = _tabledata.TablePrefix + "." + _refType.Name;
                if (!string.IsNullOrEmpty(_tabledata.TableName)) s = _tabledata.TableName;
            }

            SourceCollection = _statements.EnvironmentCode + "." + s;
            SetCollection();
        }

        private void SetCollection() { Collection = Database.GetCollection<BsonDocument>(SourceCollection); }
        public void ClearCollection(string name) { Database.GetCollection<BsonDocument>(name).DeleteMany(new BsonDocument()); }
        public void DropCollection(string name) { Database.DropCollection(name); }
        public List<TU> GetCollection<TU>(string name) { return GetAll<TU>(Database.GetCollection<BsonDocument>(name)); }
        public TU GetCollectionMember<TU>(string name, string locator) where TU : MicroEntity<TU> { return Get<TU>(Database.GetCollection<BsonDocument>(name), locator); }

        public void AddSourceCollectionSuffix(string suffix)
        {
            SourceCollection += "." + suffix;
            SetCollection();
        }
    }
}