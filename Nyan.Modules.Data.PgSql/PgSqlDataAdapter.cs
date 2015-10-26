using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Text;
using System.Data.Common;
using Nyan.Core.Modules.Data;
using Nyan.Core.Extensions;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Npgsql;
using System.Collections.Generic;

namespace Nyan.Modules.Data.PgSql
{
    public class PgSqlDataAdapter : AdapterPrimitive
    {
        public PgSqlDataAdapter()
        {
            parameterIdentifier = "@";
            useOutputParameterForInsertedKeyExtraction = false; //Some DBs may require an OUT parameter to extract the new ID. Not the case here.
            sqlTemplateInsertSingleWithReturn = "INSERT INTO {0} ({1}) VALUES ({2}) RETURNING id";
            sqlTemplateTableTruncate = "DELETE FROM {0}"; //No such thing as TRUNCATE on SQLite, but open DELETE works the same way.

            dynamicParameterType = typeof(PgSqlDynamicParameters);
        }

        public override void CheckDatabaseEntities<T>()
        {
            if (MicroEntity<T>.TableData.IsReadOnly) return;

            try
            {
                var tn = MicroEntity<T>.Statements.SchemaElements["Table"].Value;
                var sn = "nyan";
                if (MicroEntity<T>.Statements.SchemaElements.ContainsKey("Schema"))
                {
                    sn = MicroEntity<T>.Statements.SchemaElements["Schema"].Value;
                }

                // First step - ensure schema existance.

                /* try
                {
                    MicroEntity<T>.Execute("CREATE SCHEMA IF NOT EXISTS " + sn + ";");
                    Core.Settings.Current.Log.Add(typeof(T).FullName + ": Schema [" + sn + "] verified.");
                }
                catch (Exception e)
                {
                    Core.Settings.Current.Log.Add(e);
                } */

                // Second step - check if the table is there.

                var tableCount =
                    MicroEntity<T>.QuerySingleValue<int>("SELECT COUNT(*) FROM information_schema.tables WHERE TABLE_CATALOG='" + sn + "' and TABLE_TYPE='BASE TABLE' AND TABLE_SCHEMA='public' AND TABLE_NAME='" + tn + "';");

                if (tableCount != 0) return;

                Core.Settings.Current.Log.Add(typeof(T).FullName + ": Table [" + tn + "] not found.");

                var tableRender = new StringBuilder();

                tableRender.Append("CREATE TABLE " + tn + " (");

                var isFirst = true;

                foreach (var prop in typeof(T).GetProperties())
                {
                    var pType = prop.PropertyType;
                    var pDestinyType = "VARCHAR";
                    var pLength = "";
                    var pNullableSpec = "";
                    // var pAutoKeySpec = "";
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
                        if (pType == typeof(DateTime)) pDestinyType = "TIMESTAMP";
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
                            pDestinyType = " SERIAL PRIMARY KEY ";
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

                    var lengthAttribute = prop.GetCustomAttribute<StringLengthAttribute>();
                    if (lengthAttribute != null)
                    {
                        pLength = "(" + lengthAttribute.MaximumLength + ")";
                    }
                    else if (pDestinyType == "VARCHAR")
                    {
                        // Default should be 255 on PostgreSQL
                        pLength = "(255)";
                    }

                    tableRender.Append(pSourceName + " " + pDestinyType + pLength + pNullableSpec);
                }

                tableRender.Append(", RCTS TIMESTAMP DEFAULT CURRENT_TIMESTAMP");
                tableRender.Append(", RUTS TIMESTAMP DEFAULT CURRENT_TIMESTAMP");

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
                    // Method is simply not defined. Info by log here. 
                    Core.Settings.Current.Log.Add(typeof(T).FullName + ": There's no specific OnSchemaInitialization defined. Continuing...");
                }
            }
            catch (Exception e)
            {
                Core.Settings.Current.Log.Add("  Schema render Error: " + e.Message);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public override DbConnection Connection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
