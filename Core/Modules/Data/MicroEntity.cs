using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Factories;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Data.Adapter;
using Nyan.Core.Modules.Data.Connection;
using Nyan.Core.Modules.Data.Maintenance;
using Nyan.Core.Modules.Data.Operators;
using Nyan.Core.Modules.Data.Operators.AnsiSql;
using Nyan.Core.Modules.Data.Pipeline;
using Nyan.Core.Modules.Log;
using Nyan.Core.Modules.Maintenance;
using Nyan.Core.Settings;
using Factory = Nyan.Core.Modules.Data.Connection.Factory;

namespace Nyan.Core.Modules.Data
{
    /// <summary>
    ///     Lightweight, tight-coupled (1x1) Basic ORM Dapper wrapper. Provides static and instanced methods to load, update,
    ///     save and delete records from the database.
    /// </summary>
    /// <typeparam name="T">The data class that inherits from MicroEntity.</typeparam>
    /// <example>
    ///     Class Contact: MicroEntity<Contact>
    /// </example>
    public abstract class MicroEntity<T> where T : MicroEntity<T>
    {
        // ReSharper disable once StaticFieldInGenericType
        private static readonly ConcurrentDictionary<Type, MicroEntityCompiledStatements> ClassRegistration = new ConcurrentDictionary<Type, MicroEntityCompiledStatements>();

        // ReSharper disable once StaticFieldInGenericType
        private static readonly object AccessLock = new object();

        private bool _isDeleted;

        #region Maintenance

        public static DataAdapterPrimitive.ModelDefinition ModelDefinition => Statements.Adapter.GetModel<T>(Definition.DdlContent.All);

        #endregion

        public bool IsReadOnly() { return TableData.IsReadOnly; }

        private static void LogWrap(string s, Message.EContentType pType = Message.EContentType.Generic)
        {
            if (TableData.SuppressErrors) Log.System.Add(s, pType);
            else Current.Log.Add(s, pType);
        }

        private static void LogWrap(Exception e, string v)
        {
            if (TableData.SuppressErrors) Log.System.Add(e);
            else Current.Log.Add(e, v);
        }

        private static void LogWrap(Exception e)
        {
            if (TableData.SuppressErrors) Log.System.Add(e);
            else Current.Log.Add(e);
        }

        private static void LogWrap(Type type, string v)
        {
            if (TableData.SuppressErrors) Log.System.Add(v);
            else Current.Log.Add(type, v);
        }

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
            LogWrap(typeof(T).FullName + " : " + pMessage, pType);
        }

        /// <summary>
        ///     Gets an object instance of T, using the identifier to search the Primary Key.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>An object instance if the ID exists, or NULL otherwise.</returns>
        public static T Get(string identifier)
        {
            if (identifier == null) return null;

            if (Statements.IdPropertyRaw == null) throw new MissingPrimaryKeyException("Identifier not set for " + typeof(T).FullName);

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
            T ret;

            if (Statements.Interceptor != null) { ret = Statements.Interceptor.Get<T>(identifier.ToString()); }
            else
            {
                var retCol = Statements.Adapter.useNumericPrimaryKeyOnly
                    ? Query(Statements.SqlGetSingle, new { Id = Convert.ToInt32(identifier) })
                    : Query(Statements.SqlGetSingle, new { Id = identifier });

                ret = retCol.Count > 0 ? retCol[0] : null;
            }

            return ret;
        }

        /// <summary>
        /// </summary>
        /// <param name="identifiers"></param>
        /// <returns></returns>
        public static IEnumerable<T> Get(List<string> identifiers)
        {
            IEnumerable<T> ret = new List<T>();

            const int nSize = 500;
            var list = new List<List<string>>();

            for (var si = 0; si < identifiers.Count; si += nSize) list.Add(identifiers.GetRange(si, Math.Min(nSize, identifiers.Count - si)));

            var tasks = new List<Task<List<T>>>();

            if (Statements.Interceptor != null)
                ret = Statements.Interceptor.Get<T>(identifiers);
            else
            {

                foreach (var innerList in list)
                {
                    for (var index = 0; index < innerList.Count; index++) innerList[index] = innerList[index].Replace("'", "''");

                    var qryparm = "'" + string.Join("','", innerList) + "'";

                    var q = string.Format(Statements.SqlAllFieldsQueryTemplate,
                                          Statements.IdPropertyRaw + " IN (" + qryparm + ")");

                    tasks.Add(new Task<List<T>>(() => Query(q)));
                }

                foreach (var task in tasks) task.Start();

                // ReSharper disable once CoVariantArrayConversion
                Task.WaitAll(tasks.ToArray());

                ret = tasks.Aggregate(ret, (current, task) => current.Concat(task.Result));
            }

            if (!TableData.UseCaching) return ret;



            Parallel.ForEach(ret, new ParallelOptions { MaxDegreeOfParallelism = 10 }, item =>
              {
                  var id = item.GetEntityIdentifier();
                  //if (!IsCached(id))
                  Current.Cache[CacheKey(id)] = item.ToJson();
              });

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

            var querySpec = ResolveNodeType(body.NodeType, leftSideValue,
                                            Statements.Adapter.ParameterDefinition.ToString() + leftSideValue);

            var q = string.Format(Statements.SqlAllFieldsQueryTemplate, querySpec.First());

            var ret = Query(q, queryParm);
            if (!TableData.UseCaching) return ret;

            //...but it populates the cache with all the individual results, saving time for future FETCHes.
            foreach (var o in ret) Current.Cache[CacheKey(o.GetEntityIdentifier())] = o.ToJson();

            return ret;
        }

        private static void ValidateEntityState()
        {
            if (Statements.State.Status != MicroEntityCompiledStatements.EStatus.Operational &&
                Statements.State.Status != MicroEntityCompiledStatements.EStatus.Initializing)
                throw new InvalidDataException(
                    "Class is not operational: {0}, {1} ({2})".format(Statements.State.Status.ToString(),
                                                                      Statements.State.Description, Statements.State.Stack));
        }

        /// <summary>
        ///     Tries to resolve an expression.
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
                case "System.Linq.Expressions.UnaryExpression": return ResolveExpression((expression as UnaryExpression).Operand);
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
                    else yield return new SqlNotNull { FieldName = leftValue.ToString() };
                    break;
                default:
                    if (rightValue != null) yield return new SqlEqual { FieldName = leftValue.ToString(), FieldValue = rightValue };
                    else yield return new SqlNull { FieldName = leftValue.ToString() };
                    break;
            }
        }

        public static T Patch(T preRet, Dictionary<string, object> patchList)
        {
            var oOriginal = preRet.ToDictionary();

            foreach (var item in patchList) oOriginal[item.Key] = item.Value;

            return oOriginal.ToJson().FromJson<T>();
        }

        private static void ProcAfterPipeline(string action, T current, T source)
        {
            if (Statements.AfterActionPipeline.Count <= 0) return;

            foreach (var afterActionPipeline in Statements.AfterActionPipeline)
                try { afterActionPipeline.Process(action, current, source); } catch (Exception e) { Current.Log.Add(e); }
        }

        private static T ProcBeforePipeline(string action, T current, T source)
        {
            if (current == null) return null;
            if (Statements.BeforeActionPipeline.Count <= 0) return current;

            foreach (var beforeActionPipeline in Statements.BeforeActionPipeline)
                try
                {
                    if (current != null) current = beforeActionPipeline.Process(action, current, source);
                }
                catch (Exception e) { Current.Log.Add(e); }

            return current;
        }

        /// <summary>
        ///     Get all entity entries.
        /// </summary>
        /// <returns>An enumeration with all the entries from database.</returns>
        public static IEnumerable<T> Get() { return GetAll(); }

        public static IEnumerable<T> GetAll(string extraParms = null) { return GetAll<T>(extraParms); }

        public static IEnumerable<TU> GetAll<TU>(string extraParms = null)
        {
            ValidateEntityState();

            //GetAll should never use cache, always hitting the DB.
            var ret = Statements.Interceptor != null ? Statements.Interceptor.GetAll<T, TU>(extraParms) : Query<TU>(extraParms ?? Statements.SqlGetAll);

            return ret;
        }

        public static long Count() { return Statements.Interceptor?.RecordCount<T>() ?? QuerySingleValue<long>(Statements.SqlRowCount); }

        public static long Count(MicroEntityParametrizedGet qTerm, string extraParms = null)
        {
            if (Statements.Interceptor != null) return Statements.Interceptor.RecordCount<T>(qTerm, extraParms);

            var bag = GetNewDynamicParameterBag();
            bag.Add("qParm", qTerm.QueryTerm);
            var term = Statements.SqlRowCount + " WHERE " + Statements.SqlSimpleQueryTerm;
            return QuerySingleValue<long>(term, bag);
        }

        public static IEnumerable<T> Get(MicroEntityParametrizedGet parm) { return Get(parm, null); }

        public static IEnumerable<T> Get(MicroEntityParametrizedGet parm, string extraParms)
        {
            ValidateEntityState();

            var bag = GetNewDynamicParameterBag();
            List<T> ret;

            if (Statements.Interceptor != null) { ret = Statements.Interceptor.GetAll<T>(parm, extraParms); }
            else
            {
                string src;
                if (parm.QueryTerm == null) { src = Statements.SqlGetAll; }
                else
                {
                    src = string.Format(Statements.SqlAllFieldsQueryTemplate, Statements.SqlSimpleQueryTerm);
                    bag.Add("qParm", "%" + parm.QueryTerm + "%");
                }

                var op = "";
                if (parm.OrderBy != null)
                    op = src + " " + Statements.SqlOrderByCommand + " " +
                         Statements.Adapter.GetOrderByClausefromSerializedQueryParameters<T>(parm.OrderBy);
                else op = src;

                if (parm.PageIndex != -1) op = Statements.Adapter.PaginationWrapper<T>(op, parm);

                if (Current.Environment.CurrentCode == "DEV") Current.Log.Add("QRY STATEMENT: " + op);

                ret = Query(op, bag);
            }

            if (!TableData.UseCaching) return ret;

            //...populates/refresh the cache with all the individual results, saving time for future individual FETCHes.
            if (Current.Cache.OperationalStatus != EOperationalStatus.Operational) return ret;

            foreach (var o in ret) Current.Cache[CacheKey(o.GetEntityIdentifier())] = o.ToJson();
            return ret;
        }

        public static IEnumerable<TU> Get<TU>(MicroEntityParametrizedGet parm, string extraParms)
        {
            ValidateEntityState();

            List<TU> ret = null;

            if (Statements.Interceptor != null) { ret = Statements.Interceptor.GetAll<T, TU>(parm, extraParms); }
            else
            {
                var tmp = Query<TU>(Statements.SqlGetAll);
                if (parm.QueryTerm != null)
                    tmp = tmp.Where(i =>
                    {
                        var vals = i.ToDictionary()
                            .Select(ii => ii.Value?.ToString().ToLower())
                            .Where(ij => ij != null)
                            .ToList().ToJson();
                        return vals.IndexOf(parm.QueryTerm.ToLower(), StringComparison.Ordinal) != -1;
                    }).ToList();
                if (parm.PageSize != 0) tmp = tmp.Skip((int)(parm.PageIndex * parm.PageSize)).Take((int)parm.PageSize).ToList();
                ret = tmp;
            }

            return ret;
        }

        public static bool Remove(long identifier) { return Remove(identifier.ToString(CultureInfo.InvariantCulture)); }

        public static void Remove(List<T> objs)
        {
            foreach (var obj in objs) obj.Remove();
        }

        public static void RemoveAll()
        {
            if (TableData.IsReadOnly) throw new ReadOnlyException("This entity is set as read-only.");

            ProcBeforePipeline(Support.EAction.DeleteAll, null, null);

            if (Statements.Interceptor != null) Statements.Interceptor.RemoveAll<T>();
            else Execute(Statements.SqlTruncateTable);

            ProcAfterPipeline(Support.EAction.DeleteAll, null, null);

            if (!TableData.UseCaching) return;
            if (Current.Cache.OperationalStatus != EOperationalStatus.Operational) return;
            Current.Cache.RemoveAll();
        }

        public static bool Remove(string identifier)
        {
            if (TableData.IsReadOnly) throw new ReadOnlyException("This entity is set as read-only.");

            var rec = Get(identifier);
            rec = ProcBeforePipeline(Support.EAction.Remove, rec, rec);

            if (rec == null) return false;

            rec.BeforeRemove();

            if (Statements.Interceptor != null) Statements.Interceptor.Remove<T>(identifier);
            else Execute(Statements.SqlRemoveSingleParametrized, new { Id = identifier });

            rec.OnRemove();

            ProcAfterPipeline(Support.EAction.Remove, rec, rec);

            if (!TableData.UseCaching) return true;

            Current.Cache.Remove(CacheKey(identifier));

            return true;
        }

        public static void Insert(List<T> objs)
        {
            foreach (var obj in objs) obj.Insert();
        }

        public static void Update(List<T> objs) { Save(objs); }

        public class BulkOpResult
        {
            public List<T> Success;
            public List<T> Failure;
            public Dictionary<string, ControlEntry> Control = new Dictionary<string, ControlEntry>();

            public class ControlEntry
            {
                public T Current;
                public T Original;
                public bool Success = true;
                public string Message;
            }
        }

        public static BulkOpResult Save(List<T> objs)
        {
            var res = new BulkOpResult();

            if (TableData.IsReadOnly) throw new ReadOnlyException("This entity is set as read-only.");

            var proc = new List<T>();
            var noProc = new List<T>();

            var pre = new Clicker($"[{typeof(T).FullName}] PRE BULK Save", objs.Count);

            // First populate the control strut.
            res.Control = objs.ToDictionary(i => i.GetEntityIdentifier(), i => new BulkOpResult.ControlEntry() { Current = i });

            // Now obtain all original record IDs
            var allIds = res.Control.Keys.ToList();

            // Then obtain the records themselves
            var allCurrRecs = Get(allIds);

            // Populate Control with the results
            foreach (var i in allCurrRecs) { res.Control[i.GetEntityIdentifier()].Original = i; }

            foreach (var i in res.Control)
            {
                var obj = i.Value.Current;
                var rec = i.Value.Original;

                rec = ProcBeforePipeline(Support.EAction.Remove, obj, rec);

                if (rec == null)
                {
                    noProc.Add(obj);
                    i.Value.Success = false;
                    i.Value.Message = "Failed ProcBeforePipeline";
                }
                else
                {
                    proc.Add(rec);
                    rec.BeforeSave();
                    i.Value.Success = true;
                }
                pre.Click();
            };

            if (Statements.Interceptor != null)
                Statements.Interceptor.BulkSave(proc);
            else
                foreach (var obj in proc) obj.Save();

            var post = new Clicker($"[{typeof(T).FullName}] POST BULK Save", proc.Count);

            Parallel.ForEach(res.Control.Where(i => i.Value.Success), new ParallelOptions { MaxDegreeOfParallelism = 10 }, rec =>
             {
                 var id = rec.Key;
                 post.Click();
                 rec.Value.Current.OnSave(id);
                 ProcAfterPipeline(Support.EAction.Update, rec.Value.Current, rec.Value.Original);
                 if (!TableData.UseCaching) return;
                 Current.Cache.Remove(CacheKey(id));
             });

            res.Success = proc;
            res.Failure = noProc;

            return res;
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
            if (Statements.Interceptor != null) return Statements.Interceptor.ReferenceQueryByField<T>(query);

            var b = Statements.Adapter.Parameters<T>(query);
            var statement = string.Format(Statements.SqlAllFieldsQueryTemplate, b.SqlWhereClause);
            var set = Query(statement, b);
            return set;
        }

        public static IEnumerable<T> ReferenceQueryByField(string field, long id) { return ReferenceQueryByField(field, id.ToString()); }

        public static IEnumerable<T> ReferenceQueryByField(string field, string id)
        {
            if (Statements.Interceptor != null) return Statements.Interceptor.ReferenceQueryByField<T>(field, id);

            var b = GetNewDynamicParameterBag();
            b.Add(field, id);

            var statement = string.Format(Statements.SqlAllFieldsQueryTemplate, b.SqlWhereClause);
            var set = Query(statement, b);
            return set;
        }

        public static IEnumerable<T> ReferenceQueryByStringField(string field, string id) { return ReferenceQueryByField(field, id); }

        public static IEnumerable<T> QueryByWhereClause(string clause, object b = null)
        {
            var statement = string.Format(Statements.SqlAllFieldsQueryTemplate, clause);
            var set = Query(statement, b);
            return set;
        }

        public static IEnumerable<T> Query(DynamicParametersPrimitive b)
        {
            var statement = string.Format(Statements.SqlAllFieldsQueryTemplate, b.SqlWhereClause);
            var set = Query(statement, b);
            return set;
        }

        public static ParameterDefinition ParameterDefinition => Statements.Adapter.ParameterDefinition;

        public static DynamicParametersPrimitive GetNewDynamicParameterBag(bool pRaw = false) { return Statements.Adapter.Parameters<T>(null, pRaw); }

        public static bool IsCached(long identifier) { return IsCached(identifier.ToString(CultureInfo.InvariantCulture)); }

        public static bool IsCached(string identifier) { return TableData.UseCaching && Current.Cache.Contains(CacheKey(identifier)); }

        #endregion

        #region Instanced methods

        public bool IsNew()
        {
            T ignore = null;
            return IsNew(ref ignore);
        }

        public bool IsNew(ref T oProbe)
        {
            var probe = GetEntityIdentifier();

            if (!TableData.IsInsertableIdentifier)
                if (probe == "" || probe == "0")
                    return true;

            oProbe = Get(probe);
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

            if (value.IsNumeric()) value = Convert.ToInt64(value);

            var refProp = GetType().GetProperty(Statements.IdPropertyRaw);

            refProp.SetValue(oRef, Convert.ChangeType(value, refProp.PropertyType));
        }

        public bool Remove()
        {
            if (TableData.IsReadOnly) throw new ReadOnlyException("This entity is set as read-only.");

            var id = GetEntityIdentifier();

            var rec = Get(id);

            rec = ProcBeforePipeline(Support.EAction.Remove, rec, rec);

            if (rec == null) return false;

            if (Statements.Interceptor != null) Statements.Interceptor.Remove(this);
            else Execute(Statements.SqlRemoveSingleParametrized, new { id = GetEntityIdentifier() });

            var cKey = typeof(T).FullName + ":" + GetEntityIdentifier();
            Current.Cache.Remove(cKey);

            _isDeleted = true;

            OnRemove();

            ProcAfterPipeline(Support.EAction.Remove, rec, rec);

            return true;
        }

        public string Save()
        {
            if (TableData.IsReadOnly) throw new ReadOnlyException("This entity is set as read-only.");

            if (_isDeleted) return null;
            var ret = "";

            T oldRec = null;
            var isNew = IsNew(ref oldRec);

            var rec = this;
            rec = ProcBeforePipeline(isNew ? Support.EAction.Insert : Support.EAction.Update, (T)rec, oldRec);

            if (rec == null) return null;

            BeforeSave();

            if (Statements.Interceptor != null) { ret = Statements.Interceptor.Save(rec); }
            else
            {
                var obj = !isNew
                    ? Statements.Adapter.Parameters<T>(rec)
                    : Statements.Adapter.InsertableParameters<T>(rec);

                if (Statements.IdColumn == null) { Execute(Statements.SqlInsertSingle, obj); }
                else if (isNew) { ret = SaveAndGetId(obj); }
                else
                {
                    Execute(Statements.SqlUpdateSingle, obj);
                    ret = GetEntityIdentifier();
                }
            }

            if (Current.Cache.OperationalStatus == EOperationalStatus.Operational)
            {
                Current.Cache.Remove(CacheKey(ret));
                var types = Management.GetGenericsByBaseClass(typeof(T));
                foreach (var t in types)
                {
                    var key = CacheKey(t, ret);
                    Current.Cache.Remove(key);
                }
            }

            OnSave(ret);

            if (Statements.AfterActionPipeline.Count <= 0) return ret;

            rec = Get(ret);
            ProcAfterPipeline(isNew ? Support.EAction.Insert : Support.EAction.Update, (T)rec, oldRec);

            return ret;
        }

        public string SaveAndGetId()
        {
            var obj = Statements.Adapter.Parameters<T>(this);
            return SaveAndGetId(obj);
        }

        public string SaveAndGetId(DynamicParametersPrimitive obj)
        {
            if (TableData.IsReadOnly) throw new ReadOnlyException("This entity is set as read-only.");

            if (_isDeleted) return null;
            var ret = InsertAndReturnIdentifier(obj);
            SetEntityIdentifier(ret);
            return ret;
        }

        public void Insert()
        {
            if (TableData.IsReadOnly) throw new ReadOnlyException("This entity is set as read-only.");

            if (_isDeleted) return;

            if (Statements.Interceptor != null) { Statements.Interceptor.Insert(this); }
            else
            {
                var parm = TableData.IsInsertableIdentifier
                    ? Statements.Adapter.InsertableParameters<T>(this)
                    : Statements.Adapter.Parameters<T>(this);

                Execute(Statements.SqlInsertSingle, parm);
            }

            try { OnInsert(); } catch (Exception e) { Current.Log.Add(e); }
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

        public static List<List<Dictionary<string, object>>> QueryMultiple(string sqlStatement,
                                                                           object sqlParameters = null, CommandType pCommandType = CommandType.Text)
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
                LogWrap(e,
                        GetTypeName() +
                        ": Entity/Dapper Query: Error while issuing statements to the database. Statement: [" + sqlStatement
                        +
                        "]. Database: " + cDebug);
                throw new DataException(
                    GetTypeName() +
                    "Entity/Dapper Query: Error while issuing statements to the database. Error:  [" + e.Message + "].",
                    e);
            }
        }

        public static List<T> Query(string sqlStatement, object rawObject = null)
        {
            if (Statements.Interceptor != null) return Statements.Interceptor.Query<T>(sqlStatement, rawObject);

            var primitive = rawObject as DynamicParametersPrimitive;
            var obj = primitive ?? Statements.Adapter.Parameters<T>(rawObject);

            return Query(sqlStatement, obj, CommandType.Text);
        }

        public static List<T> Query(string sqlStatement, DynamicParametersPrimitive sqlParameters,
                                    CommandType pCommandType)
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
                throw new DataException(
                    GetTypeName() + " Entity/Dapper Query: Error while issuing statements to the database. Error:  [" +
                    e.Message + "].", e);
            }
        }

        private static void DumpQuery(string sqlStatement, string dbConnection, object parms, Exception e)
        {
            ValidateEntityState();

            var sguid = Identifier.MiniGuid();

            sqlStatement = sqlStatement.Replace(System.Environment.NewLine, " ").Replace("\t", " ").Trim();

            while (sqlStatement.IndexOf("  ", StringComparison.Ordinal) != -1) sqlStatement = sqlStatement.Replace("  ", " ");

            LogWrap(sguid + " Qy [" + sqlStatement + "]", Message.EContentType.Warning);
            LogWrap(sguid + " Pr [" + parms + "]", Message.EContentType.Warning);
            LogWrap(sguid + " DB [" + dbConnection + "]", Message.EContentType.Warning);

            var errRef = e;

            while (errRef != null)
            {
                var errStatement = e.Message
                    .Replace(System.Environment.NewLine, " | ")
                    .Replace("\t", " | ")
                    .Replace("\n", " | ")
                    .Trim();

                LogWrap(sguid + " EX [" + errStatement + "]", Message.EContentType.Warning);
                errRef = e.InnerException;
            }
        }

        public static void Execute(string sqlStatement, object sourceObj) { Execute(sqlStatement, Statements.Adapter.Parameters<T>(sourceObj)); }

        public static void Execute(string sqlStatement, DynamicParametersPrimitive sqlParameters = null,
                                   CommandType pCommandType = CommandType.Text)
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
                    LogWrap(e);
                    throw;
                }
            }
        }

        public static T1 QuerySingleValue<T1>(string sqlStatement, DynamicParametersPrimitive sqlParameters = null)
        {
            using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
            {
                var ret = conn.Query<T1>(sqlStatement, sqlParameters, null, true, null, CommandType.Text).FirstOrDefault();
                return ret;
            }
        }

        public static List<TU> Query<TU>(string sqlStatement) { return Query<TU>(sqlStatement, null); }

        public static List<TU> Query<TU>(string sqlStatement, object sqlParameters = null,
                                         CommandType pCommandType = CommandType.Text)
        {
            ValidateEntityState();

            var dbConn = "";

            try
            {
                using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                {
                    dbConn = conn.DataSource;

                    conn.Open();

                    var ret = pCommandType == CommandType.StoredProcedure
                        ? conn.Query<TU>(sqlStatement, sqlParameters, null, true, null, pCommandType).ToList()
                        : conn.Query<TU>(sqlStatement, sqlParameters, null, true, null, pCommandType).ToList();
                    conn.Close();

                    return ret;
                }
            }
            catch (Exception e)
            {
                DumpQuery(sqlStatement, dbConn, sqlParameters, e);

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

                    var a = p.ParameterNames.ToList();

                    foreach (var name in a)
                        try { result[name] = p.Get<dynamic>(name); } catch { result[name] = null; }
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

        public static string InsertAndReturnIdentifier(DynamicParametersPrimitive p = null,
                                                       CommandType pCommandType = CommandType.Text)
        {
            var sqlStatement = Statements.Adapter.useIndependentStatementsForKeyExtraction
                ? Statements.SqlInsertSingle
                : Statements.SqlInsertSingleWithReturn;

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
                            p.Add("newid", dbType: DynamicParametersPrimitive.DbGenericType.String,
                                  direction: ParameterDirection.Output, size: 38);
                            conn.Execute(Statements.SqlReturnNewIdentifier, p);
                            var ret = p.Get<object>("newid").ToString();

                            trans.Commit();

                            return ret;
                        }
                    }

                    if (!Statements.Adapter.UseOutputParameterForInsertedKeyExtraction) return conn.ExecuteScalar<string>(sqlStatement, p);

                    p.Add("newid", dbType: DynamicParametersPrimitive.DbGenericType.String,
                          direction: ParameterDirection.Output, size: 38);

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

        private static Dictionary<string, string> _status = new Dictionary<string, string>();

        static MicroEntity()
        {


            lock (AccessLock)
            {
                try
                {
                    // LogLocal($"Initializing {nm}", Message.EContentType.StartupSequence);
                    ClassRegistration.TryAdd(typeof(T), new MicroEntityCompiledStatements());

                    var ps = typeof(T).GetCustomAttributes(true).OfType<PipelineAttribute>().ToList();

                    //if (ps.Count != 0)
                    //    LogLocal($"PipelineAttribute: {ps.Count} items", Message.EContentType.StartupSequence);

                    Statements.BeforeActionPipeline =
                        (from pipelineAttribute in ps
                         from type in pipelineAttribute.Types
                         where typeof(IBeforeActionPipeline).IsAssignableFrom(type)
                         select (IBeforeActionPipeline)type.GetConstructor(new Type[] { }).Invoke(new object[] { }))
                        .ToList();

                    Statements.AfterActionPipeline =
                        (from pipelineAttribute in ps
                         from type in pipelineAttribute.Types
                         where typeof(IAfterActionPipeline).IsAssignableFrom(type)
                         select (IAfterActionPipeline)type.GetConstructor(new Type[] { }).Invoke(new object[] { }))
                        .ToList();

                    if (Statements.BeforeActionPipeline.Count != 0) _status["data.BeforeActionPipeline"] = Statements.BeforeActionPipeline.Select(i => i.GetType().Name).Aggregate((i, j) => i + "," + j);
                    if (Statements.AfterActionPipeline.Count != 0) _status["data.AfterActionPipeline"] = Statements.AfterActionPipeline.Select(i => i.GetType().Name).Aggregate((i, j) => i + "," + j);

                    Statements.State.Status = MicroEntityCompiledStatements.EStatus.Initializing;
                    Statements.State.Step = "Starting TableData/Statements setup";

                    if (TableData.Label != null) Statements.Label = TableData.Label;
                    else if (TableData.TableName != null) Statements.Label = TableData.TableName;
                    else Statements.Label = typeof(T).Name;

                    Statements.State.Step = "Instantiating new ColumnAttributeTypeMapper";
                    var cat = new ColumnAttributeTypeMapper<T>();
                    SqlMapper.SetTypeMap(typeof(T), cat);

                    Statements.State.Step = "Setting up Machine/Environment SI dict";

                    // _status["env.Machine"] = System.Environment.MachineName;
                    _status["env.Environment"] = Current.Environment.Current.Code;

                    Statements.State.Step = "Setting up Statements.EnvironmentCode";
                    Statements.EnvironmentCode = ResolveEnvironment();

                    if (Statements.EnvironmentCode != Current.Environment.Current.Code) { }
                        _status["env.Environment"] = Statements.EnvironmentCode + " (overriding [" + Current.Environment.Current.Code + "])";

                    //LogLocal("INIT START", Message.EContentType.Info);

                    Statements.State.Step = "Setting up Reference Bundle";

                    var refBundle = TableData.ConnectionBundleType ?? Current.GlobalConnectionBundleType;

                    var probeType = typeof(T);

                    Statements.State.Step = "Setting up PropertyFieldMap";

                    Statements.PropertyFieldMap =
                        (from pInfo in probeType.GetProperties()
                         let p1 = pInfo.GetCustomAttributes(false).OfType<ColumnAttribute>().ToList()
                         let fieldName = p1.Count != 0 ? p1[0].Name ?? pInfo.Name : pInfo.Name
                         select new KeyValuePair<string, string>(pInfo.Name, fieldName))
                        .ToDictionary(x => x.Key, x => x.Value);

                    Statements.State.Step = "Setting up PropertyLengthMap";

                    Statements.PropertyLengthMap =
                        (from pInfo in probeType.GetProperties()
                         let p1 = pInfo.GetCustomAttributes(false).OfType<ColumnAttribute>().ToList()
                         let fieldLength = p1.Count != 0 ? (p1[0].Length != 0 ? p1[0].Length : 0) : 0
                         select new KeyValuePair<string, long>(pInfo.Name, fieldLength))
                        .ToDictionary(x => x.Key, x => x.Value);

                    Statements.State.Step = "Setting up PropertySerializationMap";

                    Statements.PropertySerializationMap =
                        (from pInfo in probeType.GetProperties()
                         let p1 = pInfo.GetCustomAttributes(false).OfType<ColumnAttribute>().ToList()
                         let isSerializable = p1.Count != 0 && p1[0].Serialized
                         select new KeyValuePair<string, bool>(pInfo.Name, isSerializable))
                        .ToDictionary(x => x.Key, x => x.Value);

                    Statements.State.Step = "Setting up CredentialCypherKeys";

                    Statements.CredentialCypherKeys = TableData.CredentialCypherKeys;

                    //First, probe for a valid Connection bundle
                    if (refBundle != null)
                    {
                        Statements.State.Step = "Setting up ConnectionCypherKeys";

                        var refType = (ConnectionBundlePrimitive)Activator.CreateInstance(refBundle);

                        Statements.Bundle = refType;

                        refType.ValidateDatabase();
                        _status["data.ConnectionBundle"] = refType.GetType().Name;
                        Statements.State.Step = "Transferring configuration settings from Bundle to Entity Statements";
                        Statements.Adapter = (DataAdapterPrimitive)Activator.CreateInstance(refType.AdapterType);
                        Statements.ConnectionCypherKeys = refType.ConnectionCypherKeys;
                    }
                    else
                    {
                        Statements.State.Status = MicroEntityCompiledStatements.EStatus.CriticalFailure;
                        Statements.State.Description = "No connection bundle specified.";
                        return;
                    }

                    // Setting up any available Interceptors...
                    if (Statements.Adapter.Interceptor != null) Statements.Interceptor = Statements.Adapter.Interceptor;

                    //Then pick Credential sets

                    Statements.State.Step = "determining CredentialSets to use";
                    Statements.CredentialSet = Factory.GetCredentialSetPerConnectionBundle(Statements.Bundle, TableData.CredentialSetType);
                    Statements.CredentialCypherKeys = Statements.CredentialSet.CredentialCypherKeys;

                    _status["data.CredentialSet"] = Statements.CredentialSet?.GetType().Name;

                    var identifierColumnName = TableData.IdentifierColumnName;

                    if (identifierColumnName == null)
                    {
                        var props = probeType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(KeyAttribute), true)).ToList();
                        if (props.Count > 0) identifierColumnName = props[0].Name;
                    }

                    if (identifierColumnName != null)
                    {
                        Statements.State.Step = "Resolving Identifier";

                        var mapEntry =
                            Statements.PropertyFieldMap.FirstOrDefault(
                                p => p.Value.ToLower().Equals(identifierColumnName.ToLower()));

                        Statements.IdProperty = Statements.Adapter.ParameterDefinition + mapEntry.Key;
                        Statements.IdPropertyRaw = mapEntry.Key;
                        Statements.IdColumn = mapEntry.Value;
                    }
                    else
                    {
                        if (TableData.TableName != null)
                        {
                            _status["data.Key"] = "ERR Missing [Key] definition";
                            throw new ConfigurationErrorsException(GetTypeName() + ": Entity (with Table name {0}) is missing a [Key] definition.".format(TableData.TableName));
                        }
                    }

                    Statements.State.Step = "Preparing Schema entities";
                    Statements.Adapter.RenderSchemaEntityNames<T>();

                    if (TableData.TableName != null)
                    {
                        Statements.State.Step = "Setting SQL statements";
                        Statements.Adapter.SetSqlStatements<T>();
                        Statements.Adapter.SetSqlTerms<T>();
                    }

                    Statements.State.Step = "Checking Connection";
                    Statements.Adapter.SetConnectionString<T>();

                    _status["data.ConnectionString"] = Statements.ConnectionString.SafeArray("Data Source", "=", ";", Transformation.ESafeArrayMode.Allow);

                    Statements.State.Step = "Evaluating Interceptor";

                    if (Statements.Interceptor != null) { Statements.Interceptor.Setup<T>(Statements); }
                    else
                    {
                        using (var conn = Statements.Adapter.Connection(Statements.ConnectionString))
                        {
                            //Test Connectivity
                            conn.Open();
                            conn.Close();
                            conn.Dispose();
                        }

                        if (!TableData.IsReadOnly)
                            if (TableData.AutoGenerateMissingSchema)
                            {
                                Statements.State.Step = "Checking database entities";
                                _status["data.DatabaseAdapter"] = Statements.Adapter.GetType().Name;
                                Statements.Adapter.CheckDatabaseEntities<T>();
                            }
                    }

                    try
                    {
                        Statements.State.Step = "Calling initialization hooks";
                        OnEntityInitializationHook();

                        Statements.Interceptor?.Initialize<T>();
                    }
                    catch (Exception e)
                    {
                        var tmpWarn = typeof(T).FullName + " : Error while " + Statements.State.Step + " - " + e.Message;
                        LogWrap(tmpWarn, Message.EContentType.Warning);
                    }

                    Current.Environment.EnvironmentChanged += Environment_EnvironmentChanged;

                    _status = _status.Where(i => !string.IsNullOrWhiteSpace(i.Value)).OrderBy(i => i.Key).ToDictionary(i => i.Key, i => i.Value);

                    LogLocal(_status.ToJson(), Message.EContentType.Info);

                    Statements.State.Status = MicroEntityCompiledStatements.EStatus.Operational;
                }
                catch (Exception e)
                {
                    _status = _status.Where(i => !string.IsNullOrWhiteSpace(i.Value)).OrderBy(i => i.Key).ToDictionary(i => i.Key, i => i.Value);

                    Statements.State.Status = MicroEntityCompiledStatements.EStatus.CriticalFailure;
                    Statements.State.Description = typeof(T).FullName + " : Error while " + Statements.State.Step + " - " + e.Message;
                    Statements.State.Stack = new StackTrace(e, true).FancyString();

                    Log.System.Add(Statements.State.Description);
                    Log.System.Add("    " + Statements.ConnectionString.SafeArray("Data Source", "=", ";", Transformation.ESafeArrayMode.Allow));
                    Log.System.Add("    Environment: " + Current.Environment.CurrentCode);
                    Log.System.Add("    " + Statements.State.Stack);

                    var refEx = e;
                    while (refEx.InnerException != null)
                    {
                        refEx = e.InnerException;
                        Statements.State.Description += " / " + refEx.Message;
                    }

                    LogWrap(Statements.State.Description, Message.EContentType.Exception);

                    LogLocal("INIT FAIL " + _status.ToJson(), Message.EContentType.Warning);

                    //throw;
                }
            }
        }

        private static string ResolveEnvironment()
        {
            var envCode = TableData.PersistentEnvironmentCode;
            var mapSrc = "TableData.PersistentEnvironmentCode";

            if (envCode == null)
            {
                var mapping = EnvironmentMappingData;
                var mappedEnvCode = mapping.ContainsKey(Current.Environment.CurrentCode) ? mapping[Current.Environment.CurrentCode] : null;

                if (mappedEnvCode != null)
                {
                    mapSrc = "Mapping";
                    envCode = mappedEnvCode;
                }
            }

            if (envCode == null)
            {
                mapSrc = "Current.Environment.CurrentCode";
                envCode = Current.Environment.CurrentCode;
            }

            if (envCode == "UND")
            {
                mapSrc = "Default Environment";
                envCode = "DEV";
            }

            if (!TableData.SuppressErrors)
                _status["env.Environment"] = "[" + envCode + "] (set by " + mapSrc + ")";

            Statements.EnvironmentCode = envCode;

            return envCode;
        }

        public static MicroEntityCompiledStatements Statements => ClassRegistration[typeof(T)];

        public static MicroEntitySetupAttribute TableData => (MicroEntitySetupAttribute)
            Attribute.GetCustomAttribute(typeof(T), typeof(MicroEntitySetupAttribute));

        public static Dictionary<string, string> EnvironmentMappingData
        {
            get
            {
                var atts = Attribute.GetCustomAttributes(typeof(T), typeof(MicroEntityEnvironmentMappingAttribute))
                    .Select(i => (MicroEntityEnvironmentMappingAttribute)i)
                    .ToList();

                if (atts.Count <= 0) return new Dictionary<string, string>();

                {
                    var ret = atts.ToDictionary(i => i.Origin, i => i.Target);

                    _status["env.EnvironmentMappings"] = string.Join(";", ret.Select(x => x.Key + ">" + x.Value).ToArray());

                    return ret;
                }
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
                LogLocal("Configuration changed", Message.EContentType.Maintenance);
                Statements.Adapter.SetConnectionString<T>();
            }
            catch (Exception e) { LogWrap(e); }
        }

        // ReSharper disable once StaticFieldInGenericType

        public static string CacheKey(string key = "")
        {
            if (_cacheKeyBase != null) return _cacheKeyBase + key;
            _cacheKeyBase = typeof(T) + ":";
            return _cacheKeyBase + key;
        }

        public static string CacheKey(Type t, string key = "") { return t.FullName + ":" + key; }

        #endregion

        #region Events

        public virtual void OnSave(string newIdentifier) { }

        public virtual void BeforeSave() { }

        public virtual void BeforeInsert() { }

        public virtual void BeforeRemove() { }

        public virtual void OnRemove() { }

        public virtual void OnInsert() { }

        public static void OnSchemaInitialization() { }

        public static void OnEntityInitialization() { }

        #endregion
    }
}