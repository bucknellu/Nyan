using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Reflection;
using System.Text;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Data.Adapter;
using Nyan.Core.Settings;

namespace Nyan.Modules.Data.SQLCompact
{
    public class SqlCompactDataAdapter : DataAdapterPrimitive
    {
        public SqlCompactDataAdapter()
        {
            //SQLite implements regular ANSI SQL, so we don't to customize the base templates.

            useOutputParameterForInsertedKeyExtraction = false;
            //Some DBs may require an OUT parameter to extract the new ID.
            sqlTemplateReturnNewIdentifier = "SELECT @@IDENTITY";
            useIndependentStatementsForKeyExtraction = true;
            sqlTemplateTableTruncate = "DELETE FROM {0}"; //No such thing as TRUNCATE on SQL Compact, but open DELETE works the same way.

            dynamicParameterType = typeof(SqlCompactDynamicParameters);
        }

        public override void CheckDatabaseEntities<T>()
        {
            if (MicroEntity<T>.TableData.IsReadOnly) return;

            //First step - check if the table is there.
            try
            {
                var tn = MicroEntity<T>.Statements.SchemaElements["Table"].Value;

                var tableCount =
                    MicroEntity<T>.QuerySingleValue<int>(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + tn + "'");

                if (tableCount != 0) return;

                Current.Log.Add(typeof(T).FullName + " : Table [" + tn + "] not found.");

                var tableRender = new StringBuilder();

                tableRender.Append("CREATE TABLE " + tn + "(");

                var isFirst = true;

                //bool isRCTSFound = false;
                //bool isRUTSFound = false;

                foreach (var prop in typeof(T).GetProperties())
                {
                    var pType = prop.PropertyType;
                    var pDestinyType = "NVARCHAR(255)";
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

                        if (pType == typeof(long)) pDestinyType = "numeric";
                        if (pType == typeof(int)) pDestinyType = "INTEGER";
                        if (pType == typeof(DateTime)) pDestinyType = "DATETIME";
                        if (pType == typeof(bool)) pDestinyType = "bit";
                        if (pType == typeof(object)) pDestinyType = "image";
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
                            pAutoKeySpec = " IDENTITY PRIMARY KEY";
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
                        pDestinyType = "image";
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

                tableRender.Append(", RCTS DATETIME DEFAULT GETDATE()");
                tableRender.Append(", RUTS DATETIME DEFAULT GETDATE()");

                tableRender.Append(")");

                try
                {
                    MicroEntity<T>.Execute(tableRender.ToString());
                    Current.Log.Add(typeof(T).FullName + " : Table [" + tn + "] created.");
                }
                catch (Exception e)
                {
                    Current.Log.Add(e);
                }

                //'Event' hook for post-schema initialization procedure:
                try
                {
                    typeof(T).GetMethod("OnSchemaInitialization", BindingFlags.Public | BindingFlags.Static)
                        .Invoke(null, null);
                }
                catch { }
            }
            catch (Exception e)
            {
                Current.Log.Add("  Schema render Error: " + e.Message);
                throw;
            }
        }

        public override DbConnection Connection(string connectionString) { return new SqlCeConnection(connectionString); }

        public override void RenderSchemaEntityNames<T>()
        {
            Current.Log.Add(GetType().FullName + ": Rendering schema element names");

            var tn = MicroEntity<T>.TableData.TableName;

            if (tn == null) return;

            var trigBaseName = "TRG_" + tn.Replace("TBL_", "");
            if (trigBaseName.Length > 27)
                trigBaseName = trigBaseName.Substring(0, 27); // Oracle Schema object naming limitation

            var res = new Dictionary<string, KeyValuePair<string, string>>
            {
                {"Sequence", new KeyValuePair<string, string>("SEQUENCE", "SEQ_" + tn.Replace("TBL_", ""))},
                {"Table", new KeyValuePair<string, string>("TABLE", tn)},
                {
                    "BeforeInsertTrigger",
                    new KeyValuePair<string, string>("TRIGGER", trigBaseName + "_BI")
                },
                {
                    "BeforeUpdateTrigger",
                    new KeyValuePair<string, string>("TRIGGER", trigBaseName + "_BU")
                }
            };

            MicroEntity<T>.Statements.SchemaElements = res;
        }
    }
}