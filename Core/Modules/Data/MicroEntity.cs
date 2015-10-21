using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using Dapper;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data.Adapter;
using Nyan.Core.Modules.Data.Connection;
using System.Collections.Concurrent;

namespace Nyan.Core.Modules.Data
{

    /// <summary>
    ///     Lightweight, tight-coupled (1x1) Basic ORM Dapper wrapper. Provides static and instanced methods to load, update, save and delete records from the database.
    /// </summary>
    /// <typeparam name="T">The data class that inherits from Entity.</typeparam>
    /// <example>Class DataLayer: Entity/<DataLayer /></example>
    public class MicroEntity<T> where T : MicroEntity<T>
    {
        // ReSharper disable once StaticFieldInGenericType
        private static readonly ConcurrentDictionary<Type, MicroEntityCompiledStatements> ClassRegistration = new ConcurrentDictionary<Type, MicroEntityCompiledStatements>();

        private static readonly object AccessLock = new object();
        private bool _isDeleted;

        #region Generic methods

        #region Static Methods

        private static string _typeName;

        public static T Get(long identifier)
        {
            return Get(identifier.ToString(CultureInfo.InvariantCulture));
        }

        public static string GetTypeName()
        {
            if (_typeName != null) return _typeName;

            _typeName = typeof(T).FullName;
            return _typeName;
        }

        /// <summary>
        ///     Gets an object instance of T, using the identifier to search the Primary Key.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>An object instance if the ID exists, or NULL otherwise.</returns>
        public static T Get(string identifier)
        {
            if (Statements.IdPropertyRaw == null)
                throw new MissingPrimaryKeyException("Identifier not set for " + typeof(T).FullName);

            var ret = TableData.UseCaching
                ? Cache.Helper.FetchCacheableSingleResultByKey(GetFromDatabase, identifier)
                : GetFromDatabase(identifier);

            return ret;
        }

        /// <summary>
        ///     Forces entity lookup in database, skipping cache. 
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>An object instance if the ID exists, or NULL otherwise.</returns>
        internal static T GetFromDatabase(string identifier)
        {
            var retCol = Query(Statements.SqlGetSingle, new { Id = identifier });
            var ret = retCol.Count > 0 ? retCol[0] : null;
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifiers"></param>
        /// <returns></returns>
        public static IEnumerable<T> Get(List<string> identifiers)
        {
            IEnumerable<T> ret = new List<T>();

            const int nSize = 500;
            var list = new List<List<string>>();

            for (var si = 0; si < identifiers.Count; si += nSize)
                list.Add(identifiers.GetRange(si, Math.Min(nSize, identifiers.Count - si)));

            var tasks = new List<Task<List<T>>>();

            foreach (var innerList in list)
            {
                for (var index = 0; index < innerList.Count; index++)
                    innerList[index] = innerList[index].Replace("'", "''");

                var qryparm = "'" + string.Join("','", innerList) + "'";

                var q = string.Format(Statements.SqlAllFieldsQueryTemplate,
                    Statements.IdPropertyRaw + " IN (" + qryparm + ")");

                tasks.Add(new Task<List<T>>(() => Query(q)));
            }

            foreach (var task in tasks)
                task.Start();

            Task.WaitAll(tasks.ToArray());

            ret = tasks.Aggregate(ret, (current, task) => current.Concat(task.Result));

            if (!TableData.UseCaching) return ret;

            foreach (var item in ret)
            {
                var id = item.GetEntityIdentifier();

                if (!IsCached(id)) Core.Settings.Current.Cache[CacheKey(id)] = item.ToJson();
            }

            return ret;
        }

        public static IEnumerable<T> GetAll()
        {
            if (Statements.Status != MicroEntityCompiledStatements.EStatus.Operational)
                throw new InvalidDataException("Class is not operational: {0}, {1}".format(Statements.Status.ToString(), Statements.StatusDescription));

            //GetAll should never use cache, always hitting the DB.
            var ret = Query(Statements.SqlGetAll);
            if (!TableData.UseCaching) return ret;

            //...but it populates the cache with all the individual results, saving time for future FETCHes.
            foreach (var o in ret) Settings.Current.Cache[CacheKey(o.GetEntityIdentifier())] = o.ToJson();

            return ret;
        }

        public static void Remove(long identifier)
        {
            Remove(identifier.ToString(CultureInfo.InvariantCulture));
        }
        public static void RemoveAll()
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            Execute(Statements.SqlTruncateTable);

            if (!TableData.UseCaching) return;

            Settings.Current.Cache.RemoveAll();
        }

        public static void Remove(string identifier)
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            Execute(Statements.SqlRemoveSingleParametrized, new { Id = identifier });

            if (!TableData.UseCaching) return;

            Settings.Current.Cache.Remove(CacheKey(identifier));
        }

        public static void Insert(List<T> objs)
        {
            foreach (var obj in objs)
                obj.Insert();
        }

        public static void Update(List<T> objs)
        {
            Save(objs);
        }

        public static void Save(List<T> objs)
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            foreach (var obj in objs)
                obj.Save();
        }

        public static void Put(List<T> objs)
        {
            var updbag = objs.Where(x => !x.IsNew()).ToList();
            var insbag = objs.Where(x => x.IsNew()).ToList();

            Update(updbag);
            Insert(insbag);
        }

        public static IEnumerable<T> ReferenceQuery(object query)
        {
            var b = Statements.Adapter.Parameters<T>(query);
            var statement = string.Format(Statements.SqlAllFieldsQueryTemplate, b.SqlWhereClause);
            var set = Query(statement, b);
            return set;
        }

        public static IEnumerable<T> ReferenceQueryByField(string field, string id)
        {
            var b = GetNewDynamicParameterBag();
            b.Add(field, id, DynamicParametersPrimitive.DbGenericType.String, ParameterDirection.Input);

            var statement = string.Format(Statements.SqlAllFieldsQueryTemplate, b.SqlWhereClause);
            var set = Query(statement, b);
            return set;
        }

        public static IEnumerable<T> QueryByWhereClause(string clause)
        {
            var statement = string.Format(Statements.SqlAllFieldsQueryTemplate, clause);
            var set = Query(statement);
            return set;
        }

        public static DynamicParametersPrimitive GetNewDynamicParameterBag()
        {
            return Statements.Adapter.Parameters<T>(null);
        }

        public static bool IsCached(long identifier)
        {
            return IsCached(identifier.ToString(CultureInfo.InvariantCulture));
        }

        public static bool IsCached(string identifier)
        {
            return TableData.UseCaching && Settings.Current.Cache.Contains(CacheKey(identifier));
        }

        #endregion

        #region Instanced methods

        public bool IsNew()
        {
            var probe = GetEntityIdentifier();
            if (probe == "" || probe == "0") return true;

            var oProbe = Get(probe);
            return oProbe == null;
        }

        public string GetEntityIdentifier(MicroEntity<T> oRef = null)
        {
            if (oRef == null) oRef = this;

            return (GetType().GetProperty(Statements.IdPropertyRaw).GetValue(oRef, null) ?? "").ToString();
        }

        public void Remove()
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            Execute(Statements.SqlRemoveSingleParametrized, this);

            var cKey = typeof(T).FullName + ":" + GetEntityIdentifier();
            Settings.Current.Cache.Remove(cKey);

            //if (Cache.Contains(cKey))
            //    Cache.Remove(cKey);

            _isDeleted = true;
            OnRemove();
        }

        public string Save()
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            if (_isDeleted) return null;
            var ret = "";

            object obj = this;

            if (!IsNew())
                obj = Statements.Adapter.Parameters<T>(this);

            if (Statements.IdColumn == null)
                Execute(Statements.SqlInsertSingle, obj);
            else if (IsNew())
                ret = SaveAndGetId(obj);
            else
            {
                Execute(Statements.SqlUpdateSingle, obj);
                ret = GetEntityIdentifier();
            }

            //Log.Add(GetType().FullName + ": SAVE " + ret);

            Settings.Current.Cache.Remove(CacheKey(ret));


            //Settings.Current.Cache.Remove(CacheKey("A"));

            OnSave(ret);

            return ret;
        }

        public string SaveAndGetId()
        {
            return SaveAndGetId(this);
        }

        public string SaveAndGetId(object obj)
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            if (_isDeleted) return null;

            var ret = ExecuteAndReturnIdentifier(Statements.SqlInsertSingleWithReturn, obj);

            Settings.Current.Cache.Remove(CacheKey(ret));
            Get(ret);

            //Settings.Current.Cache.Remove(CacheKey("A"));

            return ret;
        }

        public void Insert()
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            if (_isDeleted) return;

            var parm = TableData.IsInsertableIdentifier
                ? (object)Statements.Adapter.InsertableParameters<T>(this)
                : this;

            Execute(Statements.SqlInsertSingle, parm);

            OnInsert();
        }

        #endregion

        #endregion

        #region Executors

        public static List<IDictionary<string, object>> QueryObject(string sqlStatement)
        {
            if (Statements.Status != MicroEntityCompiledStatements.EStatus.Operational)
                throw new InvalidOperationException(typeof(T).FullName + " - state is not operational: " +
                                                    Statements.StatusDescription);

            using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
            {
                conn.Open();

                var o =
                    conn.Query(sqlStatement, null, null, false, null, CommandType.Text)
                        .Select(a => (IDictionary<string, object>)a)
                        .ToList();

                conn.Close();
                conn.Dispose();

                return o;
            }
        }

        public static List<T> Query(string sqlStatement, object sqlParameters = null,
            CommandType pCommandType = CommandType.Text)
        {
            if (Statements.Status != MicroEntityCompiledStatements.EStatus.Operational)
                throw new InvalidOperationException(typeof(T).FullName + " - state is not operational: " +
                                                    Statements.StatusDescription);

            var cDebug = "";

            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    cDebug = conn.DataSource;
                    conn.Open();

                    var o =
                        conn.Query(sqlStatement, sqlParameters, null, false, null, pCommandType)
                            .Select(a => (IDictionary<string, object>)a)
                            .ToList();
                    conn.Close();
                    conn.Dispose();

                    return o.Select(refObj => refObj.GetObject<T>(Statements.PropertyFieldMap)).ToList();
                }
            }
            catch (Exception e)
            {
                Settings.Current.Log.Add(e,
                    GetTypeName() +
                    ": Entity/Dapper Query: Error while issuing statements to the database. Statement: [" + sqlStatement +
                    "]. Database: " + cDebug);
                throw new DataException(
                    GetTypeName() +
                    "Entity/Dapper Query: Error while issuing statements to the database. Error:  [" + e.Message + "].",
                    e);
            }
        }

        public static void Execute(string sqlStatement, object sqlParameters = null,
            CommandType pCommandType = CommandType.Text)
        {
            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    conn.Open();
                    conn.Execute(sqlStatement, sqlParameters, null, null, pCommandType);
                    conn.Close();
                    conn.Dispose();
                }
            }
            catch (Exception e)
            {
                throw new DataException(
                    "Entity/Dapper Execute: Error while issuing statements to the database. " +
                    "Error: [" + e.Message + "]." +
                    "Statement: [" + sqlStatement + "]. "
                    , e);
            }
        }

        public static string QuerySingleValueString(string sqlStatement)
        {
            using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
            {
                try
                {
                    var ret = conn.Query<string>(sqlStatement, null, null, true, null, CommandType.Text).First();
                    return ret;
                }
                catch (Exception e)
                {
                    Settings.Current.Log.Add(e);
                    throw (e);
                }
            }
        }

        public static T1 QuerySingleValue<T1>(string sqlStatement)
        {
            using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
            {
                var ret = conn.Query<T1>(sqlStatement, null, null, true, null, CommandType.Text).First();
                return ret;
            }
        }

        public static List<TU> Query<TU>(string sqlStatement)
        {
            return Query<TU>(sqlStatement, null);
        }

        public static List<TU> Query<TU>(string sqlStatement, object sqlParameters = null,
            CommandType pCommandType = CommandType.Text)
        {
            if (Statements.Status != MicroEntityCompiledStatements.EStatus.Operational)
                throw new InvalidOperationException(typeof(T).FullName + " - state is not operational: " +
                                                    Statements.StatusDescription);

            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    conn.Open();
                    var ret = pCommandType == CommandType.StoredProcedure
                        ? conn.Query<TU>(sqlStatement, sqlParameters, null, true, null, pCommandType).ToList()
                        : conn.Query<TU>(sqlStatement, sqlParameters, null, true, null, pCommandType).ToList<TU>();
                    conn.Close();
                    conn.Dispose();
                    return ret;
                }
            }
            catch (Exception e)
            {
                throw new DataException(
                    "Entity/Dapper Query: Error while issuing statements to the database. " +
                    "Statement: [" + sqlStatement + "]. " +
                    "Error:  [" + e.Message + "].", e);
            }
        }

        public static Dictionary<string, dynamic> ExecuteOutputParameters(string sqlStatement,
            DynamicParametersPrimitive p)
        {
            var result = new Dictionary<string, dynamic>();

            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    conn.Open();
                    conn.Execute(sqlStatement, p, commandType: CommandType.StoredProcedure);
                    conn.Close();
                    conn.Dispose();

                    Settings.Current.Log.Add(p.ParameterNames == null);

                    var a = p.ParameterNames.ToList();

                    foreach (var name in a)
                    {
                        try
                        {
                            result[name] = p.Get<dynamic>(name);
                        }
                        catch
                        {
                            result[name] = null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new DataException(
                    "Entity/Dapper Execute: Error while issuing statements to the database. " +
                    "Error:  [" + e.Message + "]." +
                    "Statement: [" + sqlStatement + "]. "
                    , e);
            }

            return result;
        }

        public static string ExecuteAndReturnIdentifier(string sqlStatement, object sqlParameters = null,
            CommandType pCommandType = CommandType.Text)
        {
            var p = Statements.Adapter.InsertableParameters<T>(sqlParameters);

            try
            {
                if (Statements.Adapter.UseOutputParameterForInsertedKeyExtraction)
                {
                    p.Add("newid", dbType: DynamicParametersPrimitive.DbGenericType.String,
                        direction: ParameterDirection.Output,
                        size: 38);

                    using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                    {
                        conn.Query<string>(sqlStatement, p, null, true, null, pCommandType);
                        var ret = p.Get<object>("newid").ToString();
                        return ret;
                    }
                }
                else
                {
                    using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                    {
                        return conn.Query<string>(sqlStatement, p).First();
                    }
                }
            }
            catch (Exception e)
            {
                throw new DataException("Entity/Dapper Query/Get: Error while issuing statements to the database. "
                                        + "Error:  [" + e.Message + "]."
                                        + "Statement: [" + sqlStatement + "]. "
                    , e);
            }
        }

        #endregion

        #region Bootstrappers

        static MicroEntity()
        {
            lock (AccessLock)
            {
                try
                {
                    Settings.Current.Log.Add("{0} @ {1} - {2} : Initializing".format(typeof(T).FullName, System.Environment.MachineName, Settings.Current.Environment.Current));

                    ClassRegistration.TryAdd(typeof(T), new MicroEntityCompiledStatements());

                    Statements.Status = MicroEntityCompiledStatements.EStatus.Initializing;
                    Statements.StatusStep = "Instantiating ColumnAttributeTypeMapper";

                    var cat = new ColumnAttributeTypeMapper<T>();
                    SqlMapper.SetTypeMap(typeof(T), cat);

                    var refBundle = TableData.ConnectionBundleType != null ? TableData.ConnectionBundleType : Settings.Current.GlobalConnectionBundleType;

                    var probeType = typeof(T);

                    Statements.PropertyFieldMap =
                        (from pInfo in
                            probeType.GetProperties()
                         let p1 = pInfo.GetCustomAttributes(false).OfType<ColumnAttribute>().ToList()
                         let fieldName = (p1.Count != 0 ? p1[0].Name : pInfo.Name)
                         select new KeyValuePair<string, string>(pInfo.Name, fieldName))
                            .ToDictionary(x => x.Key, x => x.Value);


                    //First, probe for a valid Connection bundle
                    if (refBundle != null)
                    {
                        var refType = (BundlePrimitive)Activator.CreateInstance(refBundle);

                        Statements.Bundle = refType;

                        refType.ValidateDatabase();

                        Settings.Current.Log.Add(typeof(T).FullName + " : Reading Connection bundle [" + refType.GetType().Name + "]", Log.Message.EContentType.StartupSequence);

                        Statements.StatusStep = "Transferring configuration settings from Bundle to Entity Statements";

                        Statements.Adapter = (AdapterPrimitive)Activator.CreateInstance(refType.AdapterType);
                        Statements.ConnectionCypherKeys = refType.EnvironmentCypherKeys;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("No connection bundle specified.");
                    }

                    var _IdentifierColumnName = TableData.IdentifierColumnName;

                    if (_IdentifierColumnName == null)
                    {
                        var props = probeType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(KeyAttribute))).ToList();
                        if (props.Count > 0)
                            _IdentifierColumnName = props[0].Name;
                    }

                    var mapEntry = Statements.PropertyFieldMap.First(p => p.Value.ToLower().Equals(_IdentifierColumnName.ToLower()));

                    Statements.IdProperty = Statements.Adapter.ParameterIdentifier + mapEntry.Key;
                    Statements.IdPropertyRaw = mapEntry.Key;
                    Statements.IdColumn = mapEntry.Value;

                    Statements.StatusStep = "Preparing Schema entities";
                    Statements.Adapter.RenderSchemaEntityNames<T>();

                    if (TableData.TableName != null)
                    {
                        Statements.StatusStep = "Setting SQL statements";
                        Statements.Adapter.SetSqlStatements<T>();
                    }

                    Statements.StatusStep = "Checking Connection";
                    Statements.Adapter.SetConnectionString<T>();

                    using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                    {
                        var prb = conn.GetType().Name + ": [" + conn.Database + "]";
                        try
                        {
                            var prePrb = Statements.ConnectionString.ToUpper();

                            if (prePrb.IndexOf("SERVICE_NAME", StringComparison.Ordinal) > -1)
                            {
                                prePrb =
                                    prePrb.Substring(prePrb.IndexOf("SERVICE_NAME", StringComparison.Ordinal))
                                        .Split('=')[1]
                                        .Split(')')[0];
                                prb = prePrb.ToLower();
                            }

                            if (prePrb.IndexOf("SERVER=", StringComparison.Ordinal) > -1)
                            {
                                prePrb = prePrb.Split(new[] { "SERVER=" }, StringSplitOptions.None)[1].Split(';')[0];
                                prb = prePrb.ToLower();
                            }

                            Statements.StatusStep = "Connecting to " + prb;
                        }
                        catch
                        {
                        }


                        //Test Connectivity

                        conn.Open();
                        conn.Close();
                        conn.Dispose();

                        Settings.Current.Log.Add(typeof(T).FullName + " : " + prb, Log.Message.EContentType.StartupSequence);
                    }

                    if (TableData.AutoGenerateMissingSchema)
                    {
                        Statements.StatusStep = "Checking database schema";
                        Statements.Adapter.CheckDatabaseEntities<T>();
                    }

                    Statements.StatusStep = "Calling initialization hooks";
                    OnEntityInitializationHook();

                    Settings.Current.Environment.EnvironmentChanged += Scope_EnvironmentChanged;

                    Settings.Current.Log.Add(typeof(T).FullName + ": Initialized");

                    Statements.Status = MicroEntityCompiledStatements.EStatus.Operational;
                }
                catch (Exception e)
                {
                    Statements.Status = MicroEntityCompiledStatements.EStatus.CriticalFailure;
                    Statements.StatusDescription = typeof(T).FullName + " : Error while initializing - " + e.Message;

                    var refEx = e;
                    while (refEx.InnerException != null)
                    {
                        refEx = e.InnerException;
                        Statements.StatusDescription += " / " + refEx.Message;
                    }

                    Settings.Current.Log.Add(Statements.StatusDescription, Log.Message.EContentType.Warning);
                    //throw;
                }
            }
        }

        public static MicroEntityCompiledStatements Statements
        {
            get { return ClassRegistration[typeof(T)]; }
        }

        public static MicroEntitySetupAttribute TableData
        {
            get
            {
                return (MicroEntitySetupAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(MicroEntitySetupAttribute));
            }
        }

        private static void Scope_EnvironmentChanged(object sender, EventArgs e)
        {
            //Target environment changed; pick the proper connection strings.

            HandleConfigurationChange();
        }

        private static void OnEntityInitializationHook()
        {
            //'Event' hook for post-schema initialization procedure:
            try
            {
                typeof(T).GetMethod("OnEntityInitialization", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, null);
            }
            catch
            {
            }
        }

        #endregion

        #region Configuration Helpers

        private static string _cacheKeyBase;

        private static void HandleConfigurationChange()
        {
            try
            {
                Settings.Current.Log.Add(DateTime.Now + ": " + typeof(T).FullName + " config changed.", Log.Message.EContentType.Maintenance);
                Statements.Adapter.SetConnectionString<T>();
            }
            catch (Exception e)
            {
                Settings.Current.Log.Add(e);
            }
        }


        // ReSharper disable once StaticFieldInGenericType

        public static string CacheKey(string key = "")
        {
            if (_cacheKeyBase != null)
                return _cacheKeyBase + key;

            _cacheKeyBase = typeof(T).FullName + ":";
            return _cacheKeyBase + key;
        }

        #endregion

        #region Events

        public virtual void OnSave(string newIdentifier) { }
        public virtual void OnRemove() { }
        public virtual void OnInsert() { }
        public static void OnSchemaInitialization() { }
        public static void OnEntityInitialization() { }
        #endregion

        public bool IsReadOnly()
        {
            return TableData.IsReadOnly;
        }
    }
}