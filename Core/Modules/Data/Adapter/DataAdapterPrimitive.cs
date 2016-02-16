using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Text;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Data.Adapter
{
    public abstract class DataAdapterPrimitive
    {
        // ReSharper disable InconsistentNaming
        private DynamicParametersPrimitive ParameterSourceType;
        protected internal Type dynamicParameterType = null;
        protected internal string sqlTemplateAllFieldsQuery = "SELECT * FROM {0} WHERE ({1})";
        protected internal string sqlTemplateCustomSelectQuery = "SELECT {0} FROM {2} WHERE ({1})";
        protected internal string sqlTemplateGetAll = "SELECT * FROM {0}";
        protected internal string sqlTemplateGetSingle = "SELECT * FROM {0} WHERE {1} = {2}Id";
        protected internal string sqlTemplateInsertSingle = "INSERT INTO {0} ({1}) VALUES ({2})";
        protected internal string sqlTemplateInsertSingleWithReturn = "INSERT INTO {0} ({1}) VALUES ({2}); select last_insert_rowid() as {4}newid";
        protected internal string sqlTemplateRemoveSingleParametrized = "DELETE FROM {0} WHERE {1} = {2}Id";
        protected internal string sqlTemplateReturnNewIdentifier = "select last_insert_rowid() as newid";
        protected internal string sqlTemplateTableTruncate = "TRUNCATE TABLE {0}";
        protected internal string sqlTemplateUpdateSingle = "UPDATE {0} SET {1} WHERE {2} = {3}";

        protected internal bool useIndependentStatementsForKeyExtraction = false;
        protected internal bool useNumericPrimaryKeyOnly = false;
        protected internal bool useOutputParameterForInsertedKeyExtraction = false;

        // ReSharper restore InconsistentNaming

        public bool UseOutputParameterForInsertedKeyExtraction
        {
            get { return useOutputParameterForInsertedKeyExtraction; }
        }

        public ParameterDefinition ParameterDefinition
        {
            get { return ParameterSource.ParameterDefinition; }
        }

        private DynamicParametersPrimitive ParameterSource
        {
            get
            {
                if (ParameterSourceType != null) return ParameterSourceType;

                if (dynamicParameterType == null)
                    throw new ConfigurationErrorsException("Invalid Data Adapter configuration: No Dynamic parameter type specified.");

                ParameterSourceType = (DynamicParametersPrimitive)Activator.CreateInstance(dynamicParameterType);

                return ParameterSourceType;
            }
        }

        public abstract void CheckDatabaseEntities<T>() where T : MicroEntity<T>;

        public virtual void SetSqlStatements<T>() where T : MicroEntity<T>
        {
            var tableData = MicroEntity<T>.TableData;
            var statements = MicroEntity<T>.Statements;

            var refTableName = tableData.TablePrefix + tableData.TableName;

            statements.SqlGetAll = sqlTemplateGetAll.format(refTableName);
            statements.SqlGetSingle = sqlTemplateGetSingle.format(refTableName, statements.IdColumn, ParameterDefinition);
            statements.SqlRemoveSingleParametrized = sqlTemplateRemoveSingleParametrized.format(refTableName, statements.IdColumn, ParameterDefinition);
            statements.SqlAllFieldsQueryTemplate = sqlTemplateAllFieldsQuery.format(refTableName, "{0}");
            statements.SqlCustomSelectQueryTemplate = sqlTemplateCustomSelectQuery.format("{0}", "{1}", refTableName);
            statements.SqlInsertSingle = sqlTemplateInsertSingle.format(refTableName, "{0}", "{1}");
            statements.SqlInsertSingleWithReturn = sqlTemplateInsertSingleWithReturn.format(refTableName, "{0}", "{1}", statements.IdColumn, ParameterDefinition);
            statements.SqlUpdateSingle = sqlTemplateUpdateSingle.format(refTableName, "{0}", "{1}", "{2}");
            statements.SqlRemoveSingleParametrized = sqlTemplateRemoveSingleParametrized.format(refTableName, "{0}", "{1}");
            statements.SqlTruncateTable = sqlTemplateTableTruncate.format(refTableName);
            statements.SqlReturnNewIdentifier = sqlTemplateReturnNewIdentifier;

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
                    {
                        if (field.Value.ToLower().Equals(statements.IdPropertyRaw.ToLower()))
                            canAddField = tableData.IsInsertableIdentifier;

                        if (!canAddField)
                            if (field.Key.ToLower().Equals(statements.IdPropertyRaw.ToLower()))
                                canAddField = tableData.IsInsertableIdentifier;
                    }

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
                        preInsParamList.Append(ParameterDefinition + field.Key);
                        preUpd.Append(field.Value + " = " + ParameterDefinition + field.Key);
                    }
                }
                catch (Exception e)
                {
                    Current.Log.Add("SetSqlStatements: Error rendering statements: " + e.Message,
                        Message.EContentType.Warning);
                    throw;
                }
            }

            statements.SqlInsertSingle = string.Format(statements.SqlInsertSingle, preInsFieldList, preInsParamList);
            statements.SqlInsertSingleWithReturn = string.Format(statements.SqlInsertSingleWithReturn, preInsFieldList, preInsParamList, statements.IdPropertyRaw);
            statements.SqlUpdateSingle = string.Format(statements.SqlUpdateSingle, preUpd, statements.IdColumn, statements.IdProperty);
            statements.SqlRemoveSingleParametrized = string.Format(statements.SqlRemoveSingleParametrized, statements.IdColumn, ParameterDefinition);
        }

        public virtual void SetConnectionString<T>() where T : MicroEntity<T>
        {
            var statements = MicroEntity<T>.Statements;
            var tableData = MicroEntity<T>.TableData;

            var envCode = tableData.PersistentScopeCode ?? Current.Scope.CurrentCode;

            if (envCode == "UND") envCode = "DEV";

            statements.ConnectionString = statements.ConnectionCypherKeys[envCode];

            try
            {
                // If it fails to decrypt, no biggie; It may be plain-text. ignore and continue.
                statements.ConnectionString = Current.Encryption.Decrypt(statements.ConnectionString);
            }
            catch { }

            try
            {
                // If it fails to decrypt, no biggie; It may be plain-text. ignore and continue.
                statements.CredentialsString = Current.Encryption.Decrypt(statements.CredentialsString);
            }
            catch { }



            if (statements.ConnectionString == "")
                throw new ArgumentNullException(@"Connection Cypher Key not set for " + typeof(T).FullName + ". Check class definition/configuration files.");

            if (!statements.CredentialCypherKeys.ContainsKey(envCode)) return;

            //Handling credentials

            if (statements.ConnectionString.IndexOf("{credentials}") == -1)
            {
                Current.Log.Add("[{0}] {1}: Credentials set, but no placeholder found on connection string. Skipping.".format(envCode, typeof(T).FullName), Message.EContentType.Warning);
            }

            statements.CredentialsString = statements.CredentialCypherKeys[envCode];

            try
            {
                // If it fails to decrypt, no biggie; It may be plain-text. ignore and continue.
                statements.CredentialsString = Current.Encryption.Decrypt(statements.CredentialsString);
            }
            catch { }

            statements.ConnectionString = statements.ConnectionString.Replace("{credentials}", statements.CredentialsString);
        }

        public abstract void RenderSchemaEntityNames<T>() where T : MicroEntity<T>;

        public virtual void ClearPools() { }

        public abstract DbConnection Connection(string connectionString);

        public virtual DynamicParametersPrimitive Parameters<T>(object obj, bool pRaw = false) where T : MicroEntity<T>
        {
            if (obj is DynamicParametersPrimitive) return (DynamicParametersPrimitive)obj;

            var ret = (DynamicParametersPrimitive)Activator.CreateInstance(dynamicParameterType);

            ret.SetRaw(pRaw);

            if (obj == null) return ret;

            var nonInsertableColumnsList = new List<string> { "rcts", "ruts" };

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
                    else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Fraction;
                    else if (type == typeof(int) || type == typeof(long))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Number;
                    else if (type == typeof(bool))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Bool;

                    ret.Add(pSourceName, pSourceValue, pTargetCustomType, ParameterDirection.Input);
                }
                else
                {
                    pSourceValue = pSourceValue.ToJson();
                    pTargetCustomType = DynamicParametersPrimitive.DbGenericType.String;
                    ret.Add(pSourceName, pSourceValue, pTargetCustomType, ParameterDirection.Input);
                }
            }

            return ret;
        }

        public virtual DynamicParametersPrimitive InsertableParameters<T>(object obj) where T : MicroEntity<T>
        {
            var ret = (DynamicParametersPrimitive)Activator.CreateInstance(dynamicParameterType);

            var td = MicroEntity<T>.TableData;
            var st = MicroEntity<T>.Statements;

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
                    else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Fraction;
                    else if (type == typeof(int) || type == typeof(long))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Number;
                    else if (type == typeof(bool))
                        pTargetCustomType = DynamicParametersPrimitive.DbGenericType.Bool;
                    else if (type == typeof(Guid))
                        pSourceValue = (pSourceValue == null ? null : pSourceValue.ToString());

                    ret.Add(pSourceName, pSourceValue, pTargetCustomType, ParameterDirection.Input);
                }
                else
                {
                    pSourceValue = pSourceValue != null ? pSourceValue.ToJson() : null;

                    pTargetCustomType = DynamicParametersPrimitive.DbGenericType.String;
                    ret.Add(pSourceName, pSourceValue, pTargetCustomType, ParameterDirection.Input);
                }
            }
            return ret;
        }
    }
}