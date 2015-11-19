using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Reflection;
using System.Text;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Data.Adapter;
using Nyan.Core.Settings;

namespace Nyan.Modules.Data.SQLite
{
    public class SqLiteDataAdapter : DataAdapterPrimitive
    {
        public SqLiteDataAdapter()
        {
            //SQLite implements regular ANSI SQL, so we don't to customize the base templates.

            useOutputParameterForInsertedKeyExtraction = false; //Some DBs may require an OUT parameter to extract the new ID. Not the case here.
            sqlTemplateInsertSingleWithReturn = "INSERT INTO {0} ({1}) VALUES ({2}); select last_insert_rowid() as newid";
            sqlTemplateTableTruncate = "DELETE FROM {0}"; //No such thing as TRUNCATE on SQLite, but open DELETE works the same way.

            dynamicParameterType = typeof(SqLiteDynamicParameters);
        }

        public override void CheckDatabaseEntities<T1>()
        {
            if (MicroEntity<T1>.TableData.IsReadOnly) return;

            //First step - check if the table is there.
            try
            {
                var tn = MicroEntity<T1>.Statements.SchemaElements["Table"].Value;

                var tableCount =
                    MicroEntity<T1>.QuerySingleValue<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='" + tn + "';");

                if (tableCount != 0) return;

                Current.Log.Add(typeof(T1).FullName + ": Table [" + tn + "] not found.");

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
                    MicroEntity<T1>.Execute(tableRender.ToString());
                    Current.Log.Add(typeof(T1).FullName + ": Table [" + tn + "] created.");
                }
                catch (Exception e)
                {
                    Current.Log.Add(e);
                }

                //'Event' hook for post-schema initialization procedure:
                try
                {
                    typeof(T1).GetMethod("OnSchemaInitialization", BindingFlags.Public | BindingFlags.Static)
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

        public override DbConnection Connection(string connectionString) { return new SQLiteConnection(connectionString); }
    }
}