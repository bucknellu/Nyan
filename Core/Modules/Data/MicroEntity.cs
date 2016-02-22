using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Nyan.Core.Extensions;
using Nyan.Core.Factories;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Data.Adapter;
using Nyan.Core.Modules.Data.Connection;
using Nyan.Core.Modules.Data.Operators;
using Nyan.Core.Modules.Data.Operators.AnsiSql;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using System.Diagnostics;

namespace Nyan.Core.Modules.Data
{
    /// <summary>
    ///     Lightweight, tight-coupled (1x1) Basic ORM Dapper wrapper. Provides static and instanced methods to load, update, save and delete records from the database.
    /// </summary>
    /// <typeparam name="T">The data class that inherits from Entity.</typeparam>
    /// <example>Class DataLayer: Entity/<DataLayer /></example>
    public abstract class MicroEntity<T> where T : MicroEntity<T>
    {
        // ReSharper disable once StaticFieldInGenericType
        private static readonly ConcurrentDictionary<Type, MicroEntityCompiledStatements> ClassRegistration = new ConcurrentDictionary<Type, MicroEntityCompiledStatements>();

        // ReSharper disable once StaticFieldInGenericType
        private static readonly object AccessLock = new object();

        private bool _isDeleted;

        #region Generic methods

        #region Static Methods

        // ReSharper disable once StaticFieldInGenericType
        private static string _typeName;

        public static T Get(long identifier) { return Get(identifier.ToString(CultureInfo.InvariantCulture)); }

        public static string GetTypeName()
        {
            if (_typeName != null) return _typeName;

            _typeName = typeof(T).FullName;
            return _typeName;
        }

        private static void LogLocal(string pMessage, Message.EContentType pType = Message.EContentType.Generic)
        {

            Debug.Print(pMessage);
            Current.Log.Add(typeof(T).FullName + " : " + pMessage, pType);
        }

        /// <summary>
        ///     Gets an object instance of T, using the identifier to search the Primary Key.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>An object instance if the ID exists, or NULL otherwise.</returns>
        public static T Get(string identifier)
        {
            if (identifier == null) return null;

            if (Statements.IdPropertyRaw == null)
                throw new MissingPrimaryKeyException("Identifier not set for " + typeof(T).FullName);

            var ret = TableData.UseCaching
                ? Helper.FetchCacheableSingleResultByKey(GetFromDatabase, identifier)
                : GetFromDatabase(identifier);

            return ret;
        }

        /// <summary>
        ///     Forces entity lookup in database, skipping cache. 
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>An object instance if the ID exists, or NULL otherwise.</returns>
        internal static T GetFromDatabase(object identifier)
        {
            var retCol = Statements.Adapter.useNumericPrimaryKeyOnly ? Query(Statements.SqlGetSingle, new { Id = Convert.ToInt32(identifier) }) : Query(Statements.SqlGetSingle, new { Id = identifier });

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

                var q = string.Format(Statements.SqlAllFieldsQueryTemplate, Statements.IdPropertyRaw + " IN (" + qryparm + ")");

                tasks.Add(new Task<List<T>>(() => Query(q)));
            }

            foreach (var task in tasks)
                task.Start();

            // ReSharper disable once CoVariantArrayConversion
            Task.WaitAll(tasks.ToArray());

            ret = tasks.Aggregate(ret, (current, task) => current.Concat(task.Result));

            if (!TableData.UseCaching) return ret;

            foreach (var item in ret)
            {
                var id = item.GetEntityIdentifier();

                if (!IsCached(id)) Current.Cache[CacheKey(id)] = item.ToJson();
            }

            return ret;
        }



        /// <summary>
        ///     Search for a condition in database.
        /// </summary>
        /// <param name="predicate">A lambda expression.</param>
        public static IEnumerable<T> Where(Expression<Func<T, bool>> predicate)
        {
            ValidateEntityState();

            var body = predicate.Body as BinaryExpression;

            if (body == null) return null;

            var leftSideValue = ResolveExpression(body.Left);
            var rightSideValue = ResolveExpression(body.Right);

            var queryParm = GetNewDynamicParameterBag();
            // ReSharper disable once PossibleNullReferenceException
            queryParm.Add(leftSideValue.ToString(), rightSideValue);

            var querySpec = ResolveNodeType(body.NodeType, leftSideValue, Statements.Adapter.ParameterDefinition.ToString() + leftSideValue);

            var q = string.Format(Statements.SqlAllFieldsQueryTemplate, querySpec.First());

            var ret = Query(q, queryParm);
            if (!TableData.UseCaching) return ret;

            //...but it populates the cache with all the individual results, saving time for future FETCHes.
            foreach (var o in ret) Current.Cache[CacheKey(o.GetEntityIdentifier())] = o.ToJson();

            return ret;
        }

        private static void ValidateEntityState()
        {
            if (Statements.State.Status != MicroEntityCompiledStatements.EStatus.Operational)
                throw new InvalidDataException("Class is not operational: {0}, {1} ({2})".format(Statements.State.Status.ToString(), Statements.State.Description, Statements.State.Stack));
        }

        /// <summary>
        /// Tries to resolve an expression.
        /// </summary>
        /// <param name="expression">An expression.</param>
        /// <returns>A value used to compose a search query.</returns>
        private static object ResolveExpression(Expression expression)
        {
            switch (expression.GetType().ToString())
            {
                case "System.Linq.Expressions.LogicalBinaryExpression":
                case "System.Linq.Expressions.MethodBinaryExpression":
                // TODO: Finish
                case "System.Linq.Expressions.UnaryExpression":
                    return ResolveExpression((expression as UnaryExpression).Operand);
                case "System.Linq.Expressions.PropertyExpression":
                    // return ResolveMemberExpression(expression as MemberExpression);

                    // ReSharper disable once PossibleNullReferenceException
                    return (expression as MemberExpression).Member.Name;
                case "System.Linq.Expressions.ConstantExpression":
                    // ReSharper disable once PossibleNullReferenceException
                    return (expression as ConstantExpression).Value;
                default:
                    // This will resolve NewExpression and MethodCallExpression.
                    return Expression.Lambda(expression).Compile().DynamicInvoke();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private static object ResolveMemberExpression(MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();

            return getter();
        }

        /// <summary>
        ///     Resolves an expression based on expression type, left and right values.
        /// </summary>
        /// <param name="nodeType">The ExpressionType of the node.</param>
        /// <param name="leftValue">The left value. Can be a value or enumeration.</param>
        /// <param name="rightValue">The right value. Can be a value or enumeration.</param>
        /// <returns></returns>
        private static IEnumerable<IOperator> ResolveNodeType(ExpressionType nodeType, object leftValue,
            object rightValue)
        {
            switch (nodeType)
            {
                case ExpressionType.AndAlso:
                    foreach (var leftSide in (IEnumerable<IOperator>)leftValue) yield return leftSide;
                    foreach (var rightSide in (IEnumerable<IOperator>)rightValue) yield return rightSide;
                    break;
                case ExpressionType.GreaterThan:
                    yield return new SqlGreaterThan { FieldName = leftValue.ToString(), FieldValue = rightValue };
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    yield return new SqlGreaterOrEqualThan { FieldName = leftValue.ToString(), FieldValue = rightValue };
                    break;
                case ExpressionType.LessThan:
                    yield return new SqlLessThan { FieldName = leftValue.ToString(), FieldValue = rightValue };
                    break;
                case ExpressionType.LessThanOrEqual:
                    yield return new SqlLessOrEqualThan { FieldName = leftValue.ToString(), FieldValue = rightValue };
                    break;
                // TODO: Discuss later.
                /* case ExpressionType.OrElse:
yield return new Or { OperadoresInternos = new List<IOperator> { ((IEnumerable<IOperator>)leftValue).First(), ((IEnumerable<IOperator>)rightValue).First() } };
break; */
                case ExpressionType.NotEqual:
                    if (rightValue != null) yield return new SqlNotEqual { FieldName = leftValue.ToString(), FieldValue = rightValue };
                    else
                        yield return new SqlNotNull { FieldName = leftValue.ToString() };
                    break;
                default:
                    if (rightValue != null) yield return new SqlEqual { FieldName = leftValue.ToString(), FieldValue = rightValue };
                    else
                        yield return new SqlNull { FieldName = leftValue.ToString() };
                    break;
            }
        }

        public static T Patch(T preRet, Dictionary<string, object> patchList)
        {
            var oOriginal = preRet.ToDictionary();

            foreach (var item in patchList)
                oOriginal[item.Key] = item.Value;

            return oOriginal.ToJson().FromJson<T>();
        }

        /// <summary>
        ///     Get all entity entries. 
        /// </summary>
        /// <returns>An enumeration with all the entries from database.</returns>
        public static IEnumerable<T> Get()
        {
            ValidateEntityState();

            //GetAll should never use cache, always hitting the DB.
            var ret = Query(Statements.SqlGetAll);
            if (!TableData.UseCaching) return ret;

            //...but it populates the cache with all the individual results, saving time for future FETCHes.

            if (Current.Cache.OperationalStatus != EOperationalStatus.Operational) return ret;

            foreach (var o in ret) Current.Cache[CacheKey(o.GetEntityIdentifier())] = o.ToJson();
            return ret;
        }

        public static void Remove(long identifier) { Remove(identifier.ToString(CultureInfo.InvariantCulture)); }

        public static void RemoveAll()
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            Execute(Statements.SqlTruncateTable);

            if (!TableData.UseCaching) return;
            if (Current.Cache.OperationalStatus != EOperationalStatus.Operational) return;
            Current.Cache.RemoveAll();
        }

        public static void Remove(string identifier)
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            Execute(Statements.SqlRemoveSingleParametrized, new { Id = identifier });

            if (!TableData.UseCaching) return;

            Current.Cache.Remove(CacheKey(identifier));
        }

        public static void Insert(List<T> objs)
        {
            foreach (var obj in objs)
                obj.Insert();
        }

        public static void Update(List<T> objs) { Save(objs); }

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

        public static IEnumerable<T> QueryByWhereClause(string clause, object b = null)
        {
            var statement = string.Format(Statements.SqlAllFieldsQueryTemplate, clause);
            var set = Query(statement, b);
            return set;
        }

        public static ParameterDefinition ParameterDefinition
        {
            get
            {
                return Statements.Adapter.ParameterDefinition;
            }
        }

        public static DynamicParametersPrimitive GetNewDynamicParameterBag(bool pRaw = false)
        {
            return Statements.Adapter.Parameters<T>(null, pRaw);
        }

        public static bool IsCached(long identifier) { return IsCached(identifier.ToString(CultureInfo.InvariantCulture)); }

        public static bool IsCached(string identifier) { return TableData.UseCaching && Current.Cache.Contains(CacheKey(identifier)); }

        #endregion

        #region Instanced methods

        public bool IsNew()
        {
            var probe = GetEntityIdentifier();

            if (!TableData.IsInsertableIdentifier)
                if (probe == "" || probe == "0") return true;

            var oProbe = Get(probe);
            return oProbe == null;
        }

        public string GetEntityIdentifier(MicroEntity<T> oRef = null)
        {
            if (oRef == null) oRef = this;

            return (GetType().GetProperty(Statements.IdPropertyRaw).GetValue(oRef, null) ?? "").ToString();
        }

        public void SetEntityIdentifier(object value)
        {
            var oRef = this;

            if (value.IsNumeric())
                value = Convert.ToInt64(value);

            var refProp = GetType().GetProperty(Statements.IdPropertyRaw);

            refProp.SetValue(oRef, Convert.ChangeType(value, refProp.PropertyType));
        }

        public void Remove()
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            Execute(Statements.SqlRemoveSingleParametrized, this);

            var cKey = typeof(T).FullName + ":" + GetEntityIdentifier();
            Current.Cache.Remove(cKey);

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

            DynamicParametersPrimitive obj;

            var isNew = IsNew();

            if (!isNew)
                obj = Statements.Adapter.Parameters<T>(this);
            else
            {
                obj = Statements.Adapter.InsertableParameters<T>(this);
            }

            if (Statements.IdColumn == null)
                Execute(Statements.SqlInsertSingle, obj);
            else if (isNew)
                ret = SaveAndGetId(obj);
            else
            {
                Execute(Statements.SqlUpdateSingle, obj);
                ret = GetEntityIdentifier();
            }

            //Log.Add(GetType().FullName + ": SAVE " + ret);

            Current.Cache.Remove(CacheKey(ret));

            //Settings.Current.Cache.Remove(CacheKey("A"));

            OnSave(ret);

            return ret;
        }

        public string SaveAndGetId()
        {
            var obj = Statements.Adapter.Parameters<T>(this);
            return SaveAndGetId(obj);
        }

        public string SaveAndGetId(DynamicParametersPrimitive obj)
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            if (_isDeleted) return null;
            var ret = InsertAndReturnIdentifier(obj);
            this.SetEntityIdentifier(ret);
            return ret;
        }

        public void Insert()
        {
            if (TableData.IsReadOnly)
                throw new ReadOnlyException("This entity is set as read-only.");

            if (_isDeleted) return;

            var parm = TableData.IsInsertableIdentifier
                ? Statements.Adapter.InsertableParameters<T>(this)
                : Statements.Adapter.Parameters<T>(this);

            Execute(Statements.SqlInsertSingle, parm);

            OnInsert();
        }

        #endregion

        #endregion

        #region Executors

        public static List<IDictionary<string, object>> QueryObject(string sqlStatement)
        {
            ValidateEntityState();

            using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
            {
                conn.Open();

                var o =
                    conn.Query(sqlStatement, null, null, false, null, CommandType.Text)
                        .Select(a => (IDictionary<string, object>)a)
                        .ToList();

                conn.Close();

                return o;
            }
        }

        public static List<List<Dictionary<string, object>>> QueryMultiple(string sqlStatement, object sqlParameters = null, CommandType pCommandType = CommandType.Text)
        {
            ValidateEntityState();

            var cDebug = "";

            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    cDebug = conn.DataSource;
                    conn.Open();

                    var res = new List<List<Dictionary<string, object>>>();
                    using (var m = conn.QueryMultiple(sqlStatement, sqlParameters, null, null, pCommandType))
                    {
                        while (!m.IsConsumed)
                        {
                            var probe = m.Read<object>();
                            var buffer = probe
                                .Select(a => (IDictionary<string, object>)a)
                                .Select(v => v.ToDictionary(v2 => v2.Key, v2 => v2.Value))
                                .ToList();
                            res.Add(buffer);
                        }
                    }

                    conn.Close();

                    return res;
                }
            }
            catch (Exception e)
            {
                Current.Log.Add(e, GetTypeName() + ": Entity/Dapper Query: Error while issuing statements to the database. Statement: [" + sqlStatement + "]. Database: " + cDebug);
                throw new DataException(
                    GetTypeName() +
                    "Entity/Dapper Query: Error while issuing statements to the database. Error:  [" + e.Message + "].", e);
            }
        }

        public static List<T> Query(string sqlStatement, object rawObject = null)
        {
            var primitive = rawObject as DynamicParametersPrimitive;
            DynamicParametersPrimitive obj = primitive ?? Statements.Adapter.Parameters<T>(rawObject);

            return Query(sqlStatement, obj, CommandType.Text);
        }

        public static List<T> Query(string sqlStatement, DynamicParametersPrimitive sqlParameters, CommandType pCommandType)
        {
            ValidateEntityState();

            var dbConn = "";

            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    dbConn = conn.DataSource;
                    conn.Open();

                    var o = conn.Query(sqlStatement, sqlParameters, null, false, null, pCommandType)
                        .Select(a => (IDictionary<string, object>)a)
                        .ToList();
                    conn.Close();

                    var ret = o.Select(refObj => refObj.GetObject<T>(Statements.PropertyFieldMap)).ToList();

                    return ret;
                }
            }
            catch (Exception e)
            {
                DumpQuery(sqlStatement, dbConn, sqlParameters, e);
                throw new DataException(GetTypeName() + " Entity/Dapper Query: Error while issuing statements to the database. Error:  [" + e.Message + "].", e);
            }
        }

        private static void DumpQuery(string sqlStatement, string dbConnection, object parms, Exception e)
        {
            ValidateEntityState();

            var sguid = Identifier.MiniGuid();

            sqlStatement = sqlStatement.Replace(System.Environment.NewLine, " ").Replace("\t", " ").Trim();

            while (sqlStatement.IndexOf("  ", StringComparison.Ordinal) != -1)
                sqlStatement = sqlStatement.Replace("  ", " ");

            Current.Log.Add(sguid + " Qy [" + sqlStatement + "]", Message.EContentType.Warning);
            Current.Log.Add(sguid + " Pr [" + parms.ToString() + "]", Message.EContentType.Warning);
            Current.Log.Add(sguid + " DB [" + dbConnection + "]", Message.EContentType.Warning);

            var errRef = e;

            while (errRef != null)
            {

                var errStatement = e.Message
                    .Replace(System.Environment.NewLine, " | ")
                    .Replace("\t", " | ")
                    .Replace("\n", " | ")
                    .Trim();

                Current.Log.Add(sguid + " EX [" + errStatement + "]", Message.EContentType.Warning);
                errRef = e.InnerException;

            }
        }

        public static void Execute(string sqlStatement, object sourceObj)
        {
            Execute(sqlStatement, Statements.Adapter.Parameters<T>(sourceObj));
        }

        public static void Execute(string sqlStatement, DynamicParametersPrimitive sqlParameters = null, CommandType pCommandType = CommandType.Text)
        {
            ValidateEntityState();

            var dbConn = "";

            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    dbConn = conn.DataSource;
                    conn.Open();
                    conn.Execute(sqlStatement, sqlParameters, null, null, pCommandType);
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                DumpQuery(sqlStatement, dbConn, sqlParameters, e);

                throw new DataException(GetTypeName() +
                                        " Entity /Dapper Execute: Error while issuing statements to the database. " +
                                        "Error: [" + e.Message + "]." +
                                        "Statement: [" + sqlStatement + "]. ", e);
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
                    Current.Log.Add(e);
                    throw;
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

        public static List<TU> Query<TU>(string sqlStatement) { return Query<TU>(sqlStatement, null); }

        public static List<TU> Query<TU>(string sqlStatement, object sqlParameters = null, CommandType pCommandType = CommandType.Text)
        {
            ValidateEntityState();

            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    conn.Open();
                    var ret = pCommandType == CommandType.StoredProcedure
                        ? conn.Query<TU>(sqlStatement, sqlParameters, null, true, null, pCommandType).ToList()
                        : conn.Query<TU>(sqlStatement, sqlParameters, null, true, null, pCommandType).ToList<TU>();
                    conn.Close();

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

                    Current.Log.Add(p.ParameterNames == null);

                    var a = p.ParameterNames.ToList();

                    foreach (var name in a)
                    {
                        try { result[name] = p.Get<dynamic>(name); }
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

        public static string InsertAndReturnIdentifier(DynamicParametersPrimitive p = null, CommandType pCommandType = CommandType.Text)
        {
            var sqlStatement = Statements.Adapter.useIndependentStatementsForKeyExtraction ? Statements.SqlInsertSingle : Statements.SqlInsertSingleWithReturn;

            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    if (Statements.Adapter.useIndependentStatementsForKeyExtraction)
                    {
                        conn.Open();
                        var trans = conn.BeginTransaction();

                        conn.Execute(sqlStatement, p);

                        if (!Statements.Adapter.useOutputParameterForInsertedKeyExtraction)
                        {
                            var ret = conn.ExecuteScalar<string>(Statements.SqlReturnNewIdentifier);

                            trans.Commit();

                            return ret;
                        }
                        else
                        {
                            p = GetNewDynamicParameterBag();
                            p.Add("newid", dbType: DynamicParametersPrimitive.DbGenericType.String, direction: ParameterDirection.Output, size: 38);
                            conn.Execute(Statements.SqlReturnNewIdentifier, p);
                            var ret = p.Get<object>("newid").ToString();

                            trans.Commit();

                            return ret;
                        }
                    }

                    if (!Statements.Adapter.UseOutputParameterForInsertedKeyExtraction)
                        return conn.ExecuteScalar<string>(sqlStatement, p);

                    p.Add("newid", dbType: DynamicParametersPrimitive.DbGenericType.String, direction: ParameterDirection.Output, size: 38);

                    conn.Query<string>(sqlStatement, p, null, true, null, pCommandType);
                    return p.Get<object>("newid").ToString();
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
                    ClassRegistration.TryAdd(typeof(T), new MicroEntityCompiledStatements());
                    Statements.State.Status = MicroEntityCompiledStatements.EStatus.Initializing;
                    Statements.State.Step = "Instantiating ColumnAttributeTypeMapper";

                    if (TableData.Label != null)
                        Statements.Label = TableData.Label;
                    else if (TableData.TableName != null)
                        Statements.Label = TableData.TableName;
                    else
                        Statements.Label = typeof(T).Name;

                    var cat = new ColumnAttributeTypeMapper<T>();
                    SqlMapper.SetTypeMap(typeof(T), cat);

                    Current.Log.Add("{0} : INIT START {2}".format(typeof(T).FullName, System.Environment.MachineName, Current.Environment.Current.Code != "UND" ? " (" + Current.Environment.Current + ")" : ""), Message.EContentType.Info);

                    var refBundle = TableData.ConnectionBundleType ?? Current.GlobalConnectionBundleType;

                    var probeType = typeof(T);

                    Statements.PropertyFieldMap =
                        (from pInfo in
                             probeType.GetProperties()
                         let p1 = pInfo.GetCustomAttributes(false).OfType<ColumnAttribute>().ToList()
                         let fieldName = (p1.Count != 0 ? p1[0].Name : pInfo.Name)
                         select new KeyValuePair<string, string>(pInfo.Name, fieldName))
                            .ToDictionary(x => x.Key, x => x.Value);

                    Statements.CredentialCypherKeys = TableData.CredentialCypherKeys;

                    //First, probe for a valid Connection bundle
                    if (refBundle != null)
                    {
                        var refType = (ConnectionBundlePrimitive)Activator.CreateInstance(refBundle);

                        Statements.Bundle = refType;

                        refType.ValidateDatabase();
                        LogLocal("Reading Connection bundle [" + refType.GetType().Name + "]");

                        Statements.State.Step = "Transferring configuration settings from Bundle to Entity Statements";

                        Statements.Adapter = (DataAdapterPrimitive)Activator.CreateInstance(refType.AdapterType);
                        Statements.ConnectionCypherKeys = refType.ConnectionCypherKeys;

                        if (Statements.CredentialCypherKeys.Count == 0)
                        {
                            if (refType.ConnectionCypherKeys != null)
                                Statements.CredentialCypherKeys = refType.ConnectionCypherKeys;
                        }
                        else
                        {
                            Statements.CredentialCypherKeys = new Dictionary<string, string>();
                        }

                        LogLocal(Statements.CredentialCypherKeys.Count + " CypherKeys");
                    }
                    else
                    {
                        Statements.State.Status = MicroEntityCompiledStatements.EStatus.CriticalFailure;
                        Statements.State.Description = "No connection bundle specified.";
                        return;
                    }

                    //Then pick Credential sets

                    Statements.State.Step = "determining CredentialSets to use";

                    Statements.CredentialSet = Factory.GetCredentialSetPerConnectionBundle(Statements.Bundle, TableData.CredentialSetType);
                    Statements.CredentialCypherKeys = Statements.CredentialSet.CredentialCypherKeys;

                    var identifierColumnName = TableData.IdentifierColumnName;

                    if (identifierColumnName == null)
                    {
                        var props =
                            probeType.GetProperties()
                                .Where(prop => Attribute.IsDefined(prop, typeof(KeyAttribute)))
                                .ToList();

                        LogLocal(props.ToJson());

                        if (props != null)
                            if (props.Count > 0)
                                identifierColumnName = props[0].Name;
                    }

                    if (identifierColumnName != null)
                    {

                        Statements.State.Step = "Resolving Identifier";

                        var mapEntry = Statements.PropertyFieldMap.FirstOrDefault(p => p.Value.ToLower().Equals(identifierColumnName.ToLower()));

                        LogLocal(mapEntry.ToJson());

                        Statements.IdProperty = Statements.Adapter.ParameterDefinition + mapEntry.Key;
                        Statements.IdPropertyRaw = mapEntry.Key;
                        Statements.IdColumn = mapEntry.Value;
                    }
                    else
                    {
                        if (TableData.TableName != null)
                        {
                            LogLocal("Missing [Key] definition", Message.EContentType.Warning);
                            throw new ConfigurationErrorsException(GetTypeName() + ": Entity (with Table name {0}) is missing a [Key] definition.".format(TableData.TableName));
                        }
                    }

                    Statements.State.Step = "Preparing Schema entities";
                    Statements.Adapter.RenderSchemaEntityNames<T>();

                    if (TableData.TableName != null)
                    {
                        Statements.State.Step = "Setting SQL statements";
                        Statements.Adapter.SetSqlStatements<T>();
                    }

                    Statements.State.Step = "Checking Connection";
                    Statements.Adapter.SetConnectionString<T>();

                    using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                    {
                        try
                        {
                            LogLocal(Statements.ConnectionString.SafeArray("Data Source", "=", ";", Transformation.ESafeArrayMode.Allow), Message.EContentType.MoreInfo);
                        }
                        catch { }

                        //Test Connectivity

                        conn.Open();
                        conn.Close();
                        conn.Dispose();
                    }

                    if (!TableData.IsReadOnly)
                    {
                        if (TableData.AutoGenerateMissingSchema)
                        {
                            Statements.State.Step = "Checking database entities";
                            LogLocal("schema check [" + Statements.Adapter.GetType().Name + "]");
                            Statements.Adapter.CheckDatabaseEntities<T>();
                        }
                    }

                    Statements.State.Step = "Calling initialization hooks";
                    OnEntityInitializationHook();

                    Current.Environment.EnvironmentChanged += Environment_EnvironmentChanged;

                    LogLocal("INIT OK", Message.EContentType.Info);

                    Statements.State.Status = MicroEntityCompiledStatements.EStatus.Operational;
                }
                catch (Exception e)
                {
                    Statements.State.Status = MicroEntityCompiledStatements.EStatus.CriticalFailure;
                    Statements.State.Description = typeof(T).FullName + " : Error while " + Statements.State.Step + " - " + e.Message;
                    Statements.State.Stack = new StackTrace(e, true).FancyString();

                    Log.System.Add(Statements.State.Description);
                    Log.System.Add("    " + Statements.State.Stack);

                    var refEx = e;
                    while (refEx.InnerException != null)
                    {
                        refEx = e.InnerException;
                        Statements.State.Description += " / " + refEx.Message;
                    }

                    Current.Log.Add(Statements.State.Description, Message.EContentType.Exception);

                    LogLocal("INIT FAIL", Message.EContentType.Warning);

                    //throw;
                }
            }
        }

        public static MicroEntityCompiledStatements Statements { get { return ClassRegistration[typeof(T)]; } }

        public static MicroEntitySetupAttribute TableData
        {
            get
            {
                return
                    (MicroEntitySetupAttribute)
                        Attribute.GetCustomAttribute(typeof(T), typeof(MicroEntitySetupAttribute));
            }
        }

        private static void Environment_EnvironmentChanged(object sender, EventArgs e)
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
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
        }

        #endregion

        #region Configuration Helpers

        // ReSharper disable once StaticFieldInGenericType
        private static string _cacheKeyBase;

        private static void HandleConfigurationChange()
        {
            try
            {
                LogLocal("Configuration changed config changed.", Message.EContentType.Maintenance);
                Statements.Adapter.SetConnectionString<T>();
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
            }
        }

        // ReSharper disable once StaticFieldInGenericType

        public static string CacheKey(string key = "")
        {
            if (_cacheKeyBase != null)
                return _cacheKeyBase + key;

            _cacheKeyBase = typeof(T).FullName + ":";
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

        public bool IsReadOnly() { return TableData.IsReadOnly; }
    }
}