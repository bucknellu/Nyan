using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Nyan.Core.Modules.Data;
using System.Data.SQLite;
using Nyan.Core.Extensions;
using System.Collections;
using System.Data;
using System.Reflection;

namespace Nyan.Portable.Modules.Data
{
    public class SQLiteAdapter : AdapterPrimitive
    {
        public override bool UseOutputParameterForInsertedKeyExtraction { get { return false; } }
        public override string ParameterIdentifier { get { return "@"; } }
        public override void CheckDatabaseEntities<T1>()
        {

            Core.Settings.Current.Log.Add("SQLiteAdapter: Checking database entities");

            if (MicroEntity<T1>.TableData.IsReadOnly) return;

            //First step - check if the table is there.
            try
            {
                var tn = MicroEntity<T1>.Statements.SchemaElements["Table"].Value;

                var tableCount =
                    MicroEntity<T1>.QuerySingleValue<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='" + tn + "';");

                if (tableCount != 0) return;

                Core.Settings.Current.Log.Add(typeof(T1).FullName + ": Initializing schema");


                var tableRender = new StringBuilder();

                tableRender.Append("CREATE TABLE " + tn + "(");

                var isFirst = true;

                //bool isRCTSFound = false;
                //bool isRUTSFound = false;

                foreach (var prop in typeof(T1).GetProperties())
                {
                    var pType = prop.PropertyType;
                    var pDestinyType = "VARCHAR";
                    var pNullableSpec = "";
                    var pAutoKeySpec = "";
                    var pSourceName = prop.Name;

                    if (pType.IsPrimitiveType())
                    {
                        if (pType.IsArray) continue;
                        if (!(typeof(string) == pType) && typeof(IEnumerable).IsAssignableFrom(pType)) continue;
                        if (typeof(ICollection).IsAssignableFrom(pType)) continue;
                        if (typeof(IList).IsAssignableFrom(pType)) continue;
                        if (typeof(IDictionary).IsAssignableFrom(pType)) continue;

                        if (pType.BaseType != null &&
                            (typeof(IList).IsAssignableFrom(pType.BaseType) && pType.BaseType.IsGenericType))
                            continue;

                        var isNullable = false;

                        //Check if it's a nullable type.

                        var nullProbe = Nullable.GetUnderlyingType(pType);

                        if (nullProbe != null)
                        {
                            isNullable = true;
                            pType = nullProbe;
                        }

                        if (pType == typeof(long)) pDestinyType = "NUMBER";
                        if (pType == typeof(int)) pDestinyType = "INTEGER";
                        if (pType == typeof(DateTime)) pDestinyType = "DATETIME";
                        if (pType == typeof(bool)) pDestinyType = "BOOLEAN";
                        if (pType == typeof(object)) pDestinyType = "BLOB";
                        if (pType.IsEnum) pDestinyType = "INTEGER";

                        if (pType == typeof(string)) isNullable = true;

                        if (MicroEntity<T1>.Statements.PropertyFieldMap.ContainsKey(pSourceName))
                            pSourceName = MicroEntity<T1>.Statements.PropertyFieldMap[pSourceName];

                        var bMustSkip =
                            pSourceName.ToLower().Equals("rcts") ||
                            pSourceName.ToLower().Equals("ruts");

                        if (bMustSkip) continue;

                        if (string.Equals(pSourceName, MicroEntity<T1>.Statements.IdColumn,
                            StringComparison.CurrentCultureIgnoreCase))
                        {
                            pAutoKeySpec = " PRIMARY KEY AUTOINCREMENT";
                            isNullable = false;
                        }

                        if (string.Equals(pSourceName, MicroEntity<T1>.Statements.IdPropertyRaw,
                            StringComparison.CurrentCultureIgnoreCase))
                            isNullable = false;

                        //Rendering

                        pNullableSpec = !isNullable ? " NOT NULL" : " NULL";
                    }
                    else
                    {
                        pDestinyType = "CLOB";
                        pNullableSpec = "";
                    }

                    if (!isFirst)
                        tableRender.Append(", ");
                    else
                        isFirst = false;

                    tableRender.Append(pSourceName + " " + pDestinyType + pNullableSpec + pAutoKeySpec);
                }

                //if (!isRCTSFound) tableRender.Append(", RCTS TIMESTAMP  DEFAULT CURRENT_TIMESTAMP");
                //if (!isRUTSFound) tableRender.Append(", RUTS TIMESTAMP  DEFAULT CURRENT_TIMESTAMP");

                tableRender.Append(", RCTS DATETIME DEFAULT CURRENT_TIMESTAMP");
                tableRender.Append(", RUTS DATETIME DEFAULT CURRENT_TIMESTAMP");

                tableRender.Append(")");

                try
                {
                    Core.Settings.Current.Log.Add(typeof(T1).FullName + ": Applying schema");
                    MicroEntity<T1>.Execute(tableRender.ToString());
                }
                catch (Exception e)
                {
                    Core.Settings.Current.Log.Add(e);
                }

                //'Event' hook for post-schema initialization procedure:
                try
                {
                    typeof(T1).GetMethod("OnSchemaInitialization", BindingFlags.Public | BindingFlags.Static)
                        .Invoke(null, null);
                }
                catch
                {
                }
            }
            catch (Exception e)
            {
                Core.Settings.Current.Log.Add("  Schema render Error: " + e.Message);
                throw;
            }
        }
        public override void SetSqlStatements<T1>()
        {
            var tableData = MicroEntity<T1>.TableData;
            var statements = MicroEntity<T1>.Statements;

            var refTableName = tableData.TablePrefix + tableData.TableName;

            statements.SqlGetAll =
                "SELECT * FROM " + refTableName;
            statements.SqlGetSingle =
                "SELECT * FROM " + refTableName + " WHERE " + statements.IdPropertyRaw + " = @Id";
            statements.SqlRemoveSingleParametrized =
                "DELETE FROM " + refTableName + " WHERE " + statements.IdPropertyRaw + " = @Id";
            statements.SqlAllFieldsQueryTemplate =
                "SELECT * FROM " + refTableName + " WHERE ({0})";
            statements.SqlCustomSelectQueryTemplate =
                "SELECT {0} FROM " + refTableName + " WHERE ({1})";

            var probeType = typeof(T1);
            var preInsFieldList = new StringBuilder();
            var preInsParamList = new StringBuilder();
            var preUpd = new StringBuilder();

            var banList = new List<string> { "ruts", "rcts" };

            foreach (var field in statements.PropertyFieldMap)
            {
                try
                {
                    var canAddField = true;

                    if (statements.IdPropertyRaw != null)
                        if (field.Value.ToLower().Equals(statements.IdPropertyRaw.ToLower()))
                            canAddField = tableData.IsInsertableIdentifier;


                    if (!canAddField) continue;

                    var canInsField = (!banList.Exists(t => t.ToLower().Equals(field.Value.ToLower())));

                    if (canInsField)
                    {
                        if (preInsFieldList.Length != 0)
                        {
                            preInsFieldList.Append(",");
                            preInsParamList.Append(",");
                            preUpd.Append(",");
                        }

                        preInsFieldList.Append(field.Value);
                        preInsParamList.Append("@" + field.Key);
                        preUpd.Append(field.Value + " = @" + field.Key);
                    }
                }
                catch (Exception e)
                {
                    Core.Settings.Current.Log.Add("SetSqlStatements: Error rendering statements: " + e.Message, Core.Modules.Log.Message.EContentType.Warning);
                    throw;
                }
            }


            statements.SqlInsertSingle = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", refTableName,
                preInsFieldList, preInsParamList);
            statements.SqlInsertSingleWithReturn =
                string.Format("INSERT INTO {0} ({1}) VALUES ({2}); select last_insert_rowid() as newid",
                    refTableName, preInsFieldList, preInsParamList, statements.IdPropertyRaw);
            statements.SqlUpdateSingle = string.Format("UPDATE {0} SET {1} WHERE {2} = {3}", refTableName, preUpd,
                statements.IdPropertyRaw, statements.IdProperty);
            statements.SqlRemoveSingle = string.Format("DELETE FROM {0} WHERE {1} = {2}", refTableName,
                statements.IdPropertyRaw, statements.IdProperty);
        }
        public override void SetConnectionString<T1>()
        {
            var statements = MicroEntity<T1>.Statements;
            var td = MicroEntity<T1>.TableData;

            statements.ConnectionString = statements.ConnectionCypherKeys[Core.Settings.Current.Environment.CurrentCode];

            if (statements.ConnectionString == "")
                throw new ArgumentNullException(@"Connection Cypher Key not set for " + typeof(T1).FullName + ". Check class definition/configuration files.");

            statements.ConnectionString = Core.Settings.Current.Encryption.Decrypt(statements.ConnectionString);
        }
        public override void RenderSchemaEntityNames<T>()
        {

            Core.Settings.Current.Log.Add(GetType().FullName + ": Rendering schema element names");

            var tn = MicroEntity<T>.TableData.TableName;

            if (tn == null) return;


            var res = new Dictionary<string, KeyValuePair<string, string>>
            {
                {"Table", new KeyValuePair<string, string>("TABLE", tn)},
            };

            MicroEntity<T>.Statements.SchemaElements = res;
        }
        public override void ClearPools()
        {
            throw new NotImplementedException();
        }
        public override DbConnection Connection(string connectionString)
        {
            return new SQLiteConnection(connectionString);
        }
        public override DynamicParametersPrimitive Parameters<T1>(object obj)
        {
            var ret = new SQLiteDynamicParameters();

            if (obj == null) return ret;

            var nonInsertableColumnsList = new List<string> { "rcts", "ruts" };

            foreach (var prop in obj.GetType().GetProperties())
            {
                //If field/column is banned, skip.
                if (nonInsertableColumnsList.Exists(t => t.ToLower().Equals(prop.Name.ToLower()))) continue;

                var type = prop.PropertyType;
                var pSourceName = ":" + prop.Name;
                var pSourceValue = prop.GetValue(obj, null);
                var pTargetCustomType = DynamicParametersPrimitive.DbGenericType.String;

                if (type.IsPrimitiveType())
                {

                    if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType) continue;
                    if (typeof(IDictionary<,>).IsAssignableFrom(type)) continue;
                    if (type.BaseType != null &&
                        (typeof(IList).IsAssignableFrom(type.BaseType) && type.BaseType.IsGenericType))
                        continue;


                    var nullProbe = Nullable.GetUnderlyingType(type);
                    if (nullProbe != null) type = nullProbe;

                    if (type == typeof(DateTime))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.DateTime;
                    else if (type.IsEnum)
                    {
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Number;
                        var targetType = pSourceValue.GetType();
                        pSourceValue = Convert.ToInt32(Enum.Parse(targetType, Enum.GetName(targetType, pSourceValue)));
                    }
                    else if (
                        type == typeof(double)
                        || type == typeof(float)
                        || type == typeof(decimal))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Fraction;
                    else if (
                        type == typeof(int)
                        || type == typeof(long)
                        || type == typeof(bool)
                        )
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Number;

                    ret.Add(pSourceName, pSourceValue, pTargetCustomType, ParameterDirection.Input);
                }
                else
                {
                    pSourceValue = pSourceValue.ToJson();
                    pTargetCustomType = DynamicParametersPrimitive.DbGenericType.LargeObject;
                    ret.Add(pSourceName, pSourceValue, pTargetCustomType, ParameterDirection.Input);
                }

            }

            return ret;
        }
        public override DynamicParametersPrimitive InsertableParameters<T1>(object obj)
        {
            var ret = new SQLiteDynamicParameters();

            var td = MicroEntity<T1>.TableData;
            var st = MicroEntity<T1>.Statements;

            var nonInsertableColumnsList = new List<string> { "rcts", "ruts" };

            //Banlist for insertable content. Automatic timestamps and 
            if (!td.IsInsertableIdentifier)
            {
                nonInsertableColumnsList.Add(st.IdColumn.ToLower());
                nonInsertableColumnsList.Add(st.IdPropertyRaw.ToLower());
            }

            foreach (var prop in obj.GetType().GetProperties())
            {
                //If field/column is banned, skip.
                if (nonInsertableColumnsList.Exists(t => t.ToLower().Equals(prop.Name.ToLower()))) continue;

                var type = prop.PropertyType;
                var pSourceName = prop.Name;
                var pSourceValue = prop.GetValue(obj, null);
                var pTargetCustomType = DynamicParametersPrimitive.DbGenericType.String;

                if (type.IsPrimitiveType())
                {
                    if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType) continue;
                    if (typeof(IDictionary<,>).IsAssignableFrom(type)) continue;

                    if (type.BaseType != null && (typeof(IList).IsAssignableFrom(type.BaseType) && type.BaseType.IsGenericType)) continue;

                    var nullProbe = Nullable.GetUnderlyingType(type);
                    if (nullProbe != null) type = nullProbe;

                    if (type == typeof(DateTime))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.DateTime;
                    else if (type.IsEnum)
                    {
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Number;
                        var targetType = pSourceValue.GetType();
                        pSourceValue = Convert.ToInt32(Enum.Parse(targetType, Enum.GetName(targetType, pSourceValue)));
                    }
                    else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Fraction;
                    else if (type == typeof(int) || type == typeof(long) || type == typeof(bool))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Number;
                    else if (type == typeof(Guid))
                        pSourceValue = (pSourceValue == null ? null : pSourceValue.ToString());

                    ret.Add(pSourceName, pSourceValue, pTargetCustomType, ParameterDirection.Input);
                }
                else
                {
                    pSourceValue = pSourceValue.ToJson();
                    pTargetCustomType = DynamicParametersPrimitive.DbGenericType.LargeObject;
                    ret.Add(pSourceName, pSourceValue, pTargetCustomType, ParameterDirection.Input);
                }
            }
            return ret;
        }
    }
}