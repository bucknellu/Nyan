﻿using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace Nyan.Modules.Data.SqlServer
{
    public class SqlServerDataAdapter : DataAdapterPrimitive
    {
        public SqlServerDataAdapter()
        {
            useOutputParameterForInsertedKeyExtraction = false; //Some DBs may require an OUT parameter to extract the new ID. Not the case here.
            sqlTemplateInsertSingleWithReturn = "INSERT INTO {0} ({1}) VALUES ({2}); SELECT IDENT_CURRENT ('{0}') AS as newid";
            sqlTemplateTableTruncate = "DELETE FROM {0}"; //No such thing as TRUNCATE on SQLite, but open DELETE works the same way.

            dynamicParameterType = typeof(SqlServerDynamicParameters);
        }

        public override void CheckDatabaseEntities<T>()
        {
            if (MicroEntity<T>.TableData.IsReadOnly) return;

            //First step - check if the table is there.
            try
            {
                var tn = MicroEntity<T>.Statements.SchemaElements["Table"].Value;

                var tableCount =
                    MicroEntity<T>.QuerySingleValue<int>("SELECT count(*) FROM information_schema.tables where TABLE_TYPE = 'BASE TABLE' and TABLE_NAME = '" + tn + "'; ");

                if (tableCount != 0) return;

                Core.Settings.Current.Log.Add(typeof(T).FullName + ": Table [" + tn + "] not found.");

                var tableRender = new StringBuilder();

                tableRender.Append("CREATE TABLE " + tn + "(");

                var isFirst = true;

                foreach (var prop in typeof(T).GetProperties())
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

                        if (MicroEntity<T>.Statements.PropertyFieldMap.ContainsKey(pSourceName))
                            pSourceName = MicroEntity<T>.Statements.PropertyFieldMap[pSourceName];

                        var bMustSkip =
                            pSourceName.ToLower().Equals("rcts") ||
                            pSourceName.ToLower().Equals("ruts");

                        if (bMustSkip) continue;

                        if (string.Equals(pSourceName, MicroEntity<T>.Statements.IdColumn,
                            StringComparison.CurrentCultureIgnoreCase))
                        {
                            pAutoKeySpec = " PRIMARY KEY IDENTITY";
                            isNullable = false;
                        }

                        if (string.Equals(pSourceName, MicroEntity<T>.Statements.IdPropertyRaw,
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

                tableRender.Append(", RCTS DATETIME DEFAULT GETDATE()");
                tableRender.Append(", RUTS DATETIME DEFAULT GETDATE()");

                tableRender.Append(")");

                try
                {
                    MicroEntity<T>.Execute(tableRender.ToString());
                    Core.Settings.Current.Log.Add(typeof(T).FullName + ": Table [" + tn + "] created.");
                }
                catch (Exception e)
                {
                    Core.Settings.Current.Log.Add(e);
                }

                //'Event' hook for post-schema initialization procedure:
                try
                {
                    typeof(T).GetMethod("OnSchemaInitialization", BindingFlags.Public | BindingFlags.Static)
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

        public override DbConnection Connection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        public override void RenderSchemaEntityNames<T>()
        {
            var tn = MicroEntity<T>.TableData.TableName;
            if (tn == null) return;

            var res = new Dictionary<string, KeyValuePair<string, string>>
            {
                {"Table", new KeyValuePair<string, string>("TABLE", tn)},
            };

            MicroEntity<T>.Statements.SchemaElements = res;
        }
    }
}
