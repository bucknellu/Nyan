using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Data.Adapter;
using Nyan.Core.Modules.Data.Maintenance;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Core.Wrappers;
using Oracle.ManagedDataAccess.Client;

namespace Nyan.Modules.Data.Oracle
{
    public class OracleAdapter : DataAdapterPrimitive
    {
        public OracleAdapter()
        {
            //SQLite implements regular ANSI SQL, so we don't to customize the base templates.

            useOutputParameterForInsertedKeyExtraction = true; //Some DBs may require an OUT parameter to extract the new ID. Not the case here.
            sqlTemplateInsertSingleWithReturn = "INSERT INTO {0} ({1}) VALUES ({2}) RETURNING CAST({3} AS VARCHAR2(38) ) INTO {4}newid";
            sqlTemplateTableTruncate = "TRUNCATE TABLE {0}"; //No such thing as TRUNCATE on SQLite, but open DELETE works the same way.
            dynamicParameterType = typeof(OracleDynamicParameters);
        }

        public override void CheckDatabaseEntities<T>()
        {
            if (MicroEntity<T>.TableData.IsReadOnly) return;

            //First step - check if the table is there.
            try
            {
                var tn = MicroEntity<T>.Statements.SchemaElements["Table"].Value;

                var tableCount =
                    MicroEntity<T>.QuerySingleValue<int>("SELECT COUNT(*) FROM ALL_TABLES WHERE table_name = '" + tn + "'");

                if (tableCount != 0) return;

                Current.Log.Add(typeof(T).FullName + " : Initializing schema");

                //Create sequence.
                var seqName = MicroEntity<T>.Statements.SchemaElements["Sequence"].Value;

                if (seqName.Length > 30) seqName = seqName.Substring(0, 30);

                var tableRender = new StringBuilder();

                tableRender.Append("CREATE TABLE " + tn + "(");

                var isFirst = true;

                //bool isRCTSFound = false;
                //bool isRUTSFound = false;

                foreach (var prop in typeof(T).GetProperties())
                {
                    var pType = prop.PropertyType;

                    var pSourceName = prop.Name;

                    long size = 255;

                    if (MicroEntity<T>.Statements.PropertyLengthMap.ContainsKey(pSourceName))
                    {
                        size = MicroEntity<T>.Statements.PropertyLengthMap[pSourceName];
                        if (size == 0) size = 255;

                    }

                    var pDestinyType = "VARCHAR2(" + size + ")";
                    var pNullableSpec = "";

                    if (pType.IsPrimitiveType())
                    {
                        if (pType.IsArray) continue;
                        if (!(typeof(string) == pType) && typeof(IEnumerable).IsAssignableFrom(pType)) continue;
                        if (typeof(ICollection).IsAssignableFrom(pType)) continue;
                        if (typeof(IList).IsAssignableFrom(pType)) continue;
                        if (typeof(IDictionary).IsAssignableFrom(pType)) continue;

                        if (pType.BaseType != null && typeof(IList).IsAssignableFrom(pType.BaseType) && pType.BaseType.IsGenericType) continue;

                        var isNullable = false;

                        //Check if it's a nullable type.

                        var nullProbe = Nullable.GetUnderlyingType(pType);

                        if (nullProbe != null)
                        {
                            isNullable = true;
                            pType = nullProbe;
                        }

                        if (pType == typeof(long)) pDestinyType = "NUMBER (20)";
                        if (pType == typeof(int)) pDestinyType = "NUMBER (20)";
                        if (pType == typeof(DateTime)) pDestinyType = "TIMESTAMP";
                        if (pType == typeof(bool)) pDestinyType = "NUMBER (1) DEFAULT 0";
                        if (pType == typeof(object)) pDestinyType = "BLOB";
                        if (size > 4000) pDestinyType = "BLOB";
                        if (pType.IsEnum) pDestinyType = "NUMBER (10)";

                        if (pType == typeof(string)) isNullable = true;

                        if (MicroEntity<T>.Statements.PropertyFieldMap.ContainsKey(pSourceName))
                            pSourceName = MicroEntity<T>.Statements.PropertyFieldMap[pSourceName];

                        var bMustSkip =
                            pSourceName.ToLower().Equals("rcts") ||
                            pSourceName.ToLower().Equals("ruts");

                        if (bMustSkip) continue;

                        if (string.Equals(pSourceName, MicroEntity<T>.Statements.IdColumn,
                            StringComparison.CurrentCultureIgnoreCase))
                            isNullable = false;

                        if (string.Equals(pSourceName, MicroEntity<T>.Statements.IdPropertyRaw,
                            StringComparison.CurrentCultureIgnoreCase))
                            isNullable = false;

                        //Rendering

                        if (!isNullable) pNullableSpec = " NOT NULL";
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

                    tableRender.Append(pSourceName + " " + pDestinyType + pNullableSpec);
                }

                //if (!isRCTSFound) tableRender.Append(", RCTS TIMESTAMP  DEFAULT CURRENT_TIMESTAMP");
                //if (!isRUTSFound) tableRender.Append(", RUTS TIMESTAMP  DEFAULT CURRENT_TIMESTAMP");

                tableRender.Append(", RCTS TIMESTAMP  DEFAULT CURRENT_TIMESTAMP");
                tableRender.Append(", RUTS TIMESTAMP  DEFAULT CURRENT_TIMESTAMP");

                tableRender.Append(")");

                try
                {
                    Current.Log.Add(typeof(T).FullName + " : Creating table " + tn);
                    MicroEntity<T>.Execute(tableRender.ToString());
                }
                catch (Exception e)
                {
                    Current.Log.Add(e);
                }

                if (MicroEntity<T>.Statements.IdColumn != null)
                {
                    try
                    {
                        MicroEntity<T>.Execute("DROP SEQUENCE " + seqName);
                    }
                    catch { }

                    try
                    {
                        Current.Log.Add(typeof(T).FullName + " : Creating Sequence " + seqName);
                        MicroEntity<T>.Execute("CREATE SEQUENCE " + seqName);
                    }
                    catch (Exception e)
                    {
                        Current.Log.Add(e);
                    }

                    //Primary Key
                    var pkName = tn + "_PK";
                    var pkStat = "ALTER TABLE " + tn + " ADD (CONSTRAINT " + pkName + " PRIMARY KEY (" + MicroEntity<T>.Statements.IdColumn + "))";

                    try
                    {
                        Current.Log.Add(typeof(T).FullName + " : Adding Primary Key constraint " + pkName + " (" + MicroEntity<T>.Statements.IdColumn + ")");
                        MicroEntity<T>.Execute(pkStat);
                    }
                    catch (Exception e)
                    {
                        Current.Log.Add(e);
                    }
                }
                //Trigger

                var trigStat =
                    @"CREATE OR REPLACE TRIGGER {0}
                BEFORE INSERT ON {1}
                FOR EACH ROW
                BEGIN
                " +
                    (MicroEntity<T>.Statements.IdColumn != null
                        ? @"IF :new.{3} is null 
                    THEN       
                        SELECT {2}.NEXTVAL INTO :new.{3} FROM dual;
                    END IF;"
                        : "")
                    + @"  
                :new.RCTS := CURRENT_TIMESTAMP;
                :new.RUTS := CURRENT_TIMESTAMP;
                END;";

                try
                {
                    Current.Log.Add(typeof(T).FullName + " : Adding BI Trigger " + MicroEntity<T>.Statements.SchemaElements["BeforeInsertTrigger"].Value);
                    MicroEntity<T>.Execute(
                        string.Format(trigStat,
                            MicroEntity<T>.Statements.SchemaElements["BeforeInsertTrigger"].Value, tn, seqName,
                            MicroEntity<T>.Statements.IdColumn));
                }
                catch (Exception e)
                {
                    Current.Log.Add(e);
                }

                trigStat =
                    @"CREATE OR REPLACE TRIGGER {0}
                BEFORE UPDATE ON {1}
                FOR EACH ROW
                BEGIN
                :new.RUTS := CURRENT_TIMESTAMP;
                END;";

                try
                {
                    Current.Log.Add(typeof(T).FullName + " : Adding BU Trigger " + MicroEntity<T>.Statements.SchemaElements["BeforeUpdateTrigger"].Value);

                    MicroEntity<T>.Execute(string.Format(trigStat,
                        MicroEntity<T>.Statements.SchemaElements["BeforeUpdateTrigger"].Value, tn, seqName,
                        MicroEntity<T>.Statements.IdColumn));
                }
                catch (Exception e)
                {
                    Current.Log.Add(e);
                }

                //Now, add comments to everything.

                var ocld = " - ";
                var ocfld = ";";
                var commentStat =
                    "COMMENT ON TABLE " + tn + " IS 'Auto-generated table for Entity " + typeof(T).FullName + ". " +
                    "Supporting structures - " + 
                    " Sequence: " + seqName + "; " + 
                    " Triggers: " +
                    MicroEntity<T>.Statements.SchemaElements["BeforeInsertTrigger"].Value
                    + ", " +
                    MicroEntity<T>.Statements.SchemaElements["BeforeUpdateTrigger"].Value
                    + ".'" + ocfld;

                try
                {
                    MicroEntity<T>.Execute(commentStat);
                }
                catch { }

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
                Current.Log.Add("  Schema render Error: ", Message.EContentType.Warning);
                Current.Log.Add(e);
                throw;
            }
        }

        public override DbConnection Connection(string connectionString) { return new OracleConnection(connectionString); }

        public override void RenderSchemaEntityNames<T>()
        {
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

        public override ModelDefinition GetModelfromStatements<T>(MicroEntityCompiledStatements refs, Definition.DdlContent scope)
        {
            dynamic refType = new StaticMembersDynamicWrapper(typeof(T));

            var ret = new ModelDefinition {AdapterType = "Oracle"};

            var doSchema = (scope & Definition.DdlContent.Schema) == Definition.DdlContent.Schema;
            var doData = (scope & Definition.DdlContent.Data) == Definition.DdlContent.Data;

            var schema = new StringBuilder();
            var data = new StringBuilder();

            if (doSchema)
            {
                schema.AppendLine("   DROP TABLE " + refs.SchemaElements["Table"].Value + ";");
                schema.AppendLine("   DROP SEQUENCE " + refs.SchemaElements["Sequence"].Value + ";");

                foreach (var schemaElement in refs.SchemaElements)
                {
                    var terms =
                        "SELECT DBMS_METADATA.GET_DDL('{0}', '{1}') from dual".format(
                            schemaElement.Value.Key,
                            schemaElement.Value.Value);

                    var logRef = typeof(T).FullName + ": " + schemaElement.Key;

                    try
                    {
                        string fetch = refType.QuerySingleValueString(terms);

                        if (fetch.IndexOf("USING INDEX", StringComparison.Ordinal) != -1)
                            fetch = fetch.Substring(0, fetch.IndexOf("USING INDEX", StringComparison.Ordinal)) + ")";

                        if (fetch.IndexOf("SEGMENT CREATION DEFERRED", StringComparison.Ordinal) != -1)
                            fetch = fetch.Substring(0, fetch.IndexOf("SEGMENT CREATION DEFERRED", StringComparison.Ordinal));

                        if (fetch.IndexOf("ALTER TRIGGER", StringComparison.Ordinal) != -1)
                            fetch = fetch.Substring(0, fetch.IndexOf("END;", StringComparison.Ordinal)) + "END";

                        //Current.Log.Add(logRef, Message.EContentType.Maintenance);

                        schema.AppendLine("-- =========================================================================");
                        schema.AppendLine("-- " + logRef);
                        schema.AppendLine("-- =========================================================================");
                        schema.AppendLine(fetch.Replace("\n", Environment.NewLine) + ";" + Environment.NewLine + "/" + Environment.NewLine + Environment.NewLine);
                    }
                    catch (Exception e)
                    {
                        var le = "{2} - Exception while fetching {0} {1}: {3}".format(schemaElement.Value.Key, schemaElement.Value.Value, refs.Label, e.Message);
                        Current.Log.Add(le, Message.EContentType.Warning);
                    }
                }

                ret.Schema = schema.ToString();
                ret.Available = true;
            }

            if (!doData) return ret;
            {
                Current.Log.Add("Dumping " + refs.SchemaElements["Table"].Value, Message.EContentType.Maintenance);

                var anyRows = false;

                var colSql = "SELECT COLUMN_NAME, DATA_TYPE FROM user_tab_cols WHERE table_name = '" +
                             refs.SchemaElements["Table"].Value +
                             "'AND DATA_TYPE != 'RAW'";
                List<IDictionary<string, object>> cols = refType.QueryObject(colSql);

                var canBuffer = cols.All(o => (string)o["DATA_TYPE"] != "CLOB");

                //var ctsSql =
                //    "SELECT c.owner O, c.constraint_name N FROM user_constraints c, user_tables t WHERE c.table_name = t.table_name AND c.table_name = '" +
                //    refs.SchemaElements["Table"].Value +
                //    "' AND c.status = 'ENABLED' ORDER BY c.constraint_type DESC";

                //List<IDictionary<string, object>> constraints = refType.QueryObject(ctsSql);

                var colList = cols.Select(a => a["COLUMN_NAME"]).ToList();

                var colDelimiter = new Dictionary<string, string>();

                schema.AppendLine("SET DEFINE OFF;" + Environment.NewLine);

                foreach (var col in cols)
                {
                    var delimiter = "";

                    if (col["DATA_TYPE"].ToString().IndexOf("VARCHAR", StringComparison.Ordinal) != -1)
                        delimiter = "'";

                    if (col["DATA_TYPE"].ToString().IndexOf("CLOB", StringComparison.Ordinal) != -1)
                        delimiter = "'";


                    colDelimiter.Add(col["COLUMN_NAME"].ToString(), delimiter);
                }

                var fields = string.Join(",", colList);

                try
                {
                    var recSql = "SELECT " + fields + " FROM " + refs.SchemaElements["Table"].Value;
                    List<IDictionary<string, object>> rows = refType.QueryObject(recSql);

                    Current.Log.Add("   RowCount: " + rows.Count, Message.EContentType.Maintenance);
                    var rc = 0;

                    var buffer = new StringBuilder();

                    if (canBuffer) buffer.AppendLine("INSERT ALL");

                    foreach (var row in rows)
                    {
                        rc++;

                        if ((rc % 64) == 0)
                        {
                            var stepLabel = "{0} / {1} ({2}%)".format(rc, rows.Count,
                                Convert.ToInt64((rc / (double)rows.Count) * 100));

                            Current.Log.Add("   " + stepLabel, Message.EContentType.Maintenance);

                            if (canBuffer)
                            {
                                buffer.AppendLine("SELECT * FROM dual;" + Environment.NewLine);
                                buffer.AppendLine("-- " + stepLabel);
                                buffer.AppendLine("INSERT ALL");
                            }
                        }

                        var insSql = (canBuffer ? "    INTO " : "    INSERT INTO ") +
                                     refs.SchemaElements["Table"].Value + " (" + fields +
                                     ") VALUES (";

                        var valList = "";

                        foreach (var col in cols)
                        {
                            if (valList != "") valList += ",";

                            var val = row[col["COLUMN_NAME"].ToString()];
                            var valType = col["DATA_TYPE"].ToString();
                            var delim = colDelimiter[col["COLUMN_NAME"].ToString()];

                            if (val == null)
                            {
                                valList += "NULL";
                            }
                            else
                            {
                                if (valType.IndexOf("TIMESTAMP", StringComparison.Ordinal) != -1)
                                    valList += "TO_DATE('" + ((DateTime)val).ToString("dd/MM/yyyy HH:mm:ss") + "','DD/MM/YYYY HH24:MI:SS')";

                                else if (valType.IndexOf("DATE", StringComparison.Ordinal) != -1)
                                    valList += "TO_DATE('" + ((DateTime)val).ToString("dd/MM/yyyy 00:00:00") + "','DD/MM/YYYY HH24:MI:SS')";
                                else if (valType.IndexOf("CLOB", StringComparison.Ordinal) != -1)
                                {
                                    var splitpart = val.ToString().SplitInChunksUpTo(1800).ToList();

                                    for (var i = 0; i < splitpart.Count; i++)
                                        splitpart[i] = splitpart[i].Replace("'", "''").Replace("&", "&'||'");

                                    valList += Environment.NewLine +
                                               "    TO_CLOB('') ||" + Environment.NewLine +
                                               "        '" +
                                               String.Join("' ||" + Environment.NewLine + "        '",
                                                   splitpart.ToArray()) + "'" + Environment.NewLine +
                                               "    ";
                                }
                                else
                                    valList += delim + val.ToString().Replace("'", "''").Replace("&", "&'||'") + delim;
                            }
                        }

                        anyRows = true;

                        insSql += valList + ")" + (canBuffer ? "" : ";");

                        buffer.AppendLine(insSql);
                    }

                    if (anyRows)
                    {
                        data.AppendLine("-- =========================================================================");
                        data.AppendLine("-- Populating " + refs.SchemaElements["Table"].Value);
                        data.AppendLine("-- =========================================================================");

                        data.AppendLine("TRUNCATE TABLE " + refs.SchemaElements["Table"].Value + ";" + Environment.NewLine);
                        data.AppendLine("ALTER TRIGGER \"" + refs.SchemaElements["BeforeInsertTrigger"].Value + "\" DISABLE;");
                        data.AppendLine("ALTER TRIGGER \"" + refs.SchemaElements["BeforeUpdateTrigger"].Value + "\" DISABLE;");

                        //foreach (var constraint in constraints)
                        //    data.AppendLine("ALTER TABLE " + refs.SchemaElements["Table"].Value + " DISABLE CONSTRAINT " + constraint["N"] + ";");

                        data.AppendLine(Environment.NewLine);

                        data.AppendLine(buffer.ToString());

                        if (canBuffer)
                            data.AppendLine("SELECT * FROM dual;" + Environment.NewLine);

                        data.AppendLine("COMMIT;");
                        data.AppendLine("SET DEFINE ON;");

                        //foreach (var constraint in constraints)
                        //    data.AppendLine("ALTER TABLE " + refs.SchemaElements["Table"].Value + " ENABLE CONSTRAINT " + constraint["N"] + ";");

                        data.AppendLine("ALTER TRIGGER \"" + refs.SchemaElements["BeforeInsertTrigger"].Value + "\" ENABLE;");
                        data.AppendLine("ALTER TRIGGER \"" + refs.SchemaElements["BeforeUpdateTrigger"].Value + "\" ENABLE;");

                        data.AppendLine(
                            "-- =========================================================================" + Environment.NewLine + Environment.NewLine);
                    }

                    ret.Data = data.ToString();
                    ret.Available = true;
                }
                catch (Exception e)
                {
                    Current.Log.Add("Failed to read table: {e}".format(e.Message), Message.EContentType.Warning);
                }
            }
            return ret;
        }
    }
}