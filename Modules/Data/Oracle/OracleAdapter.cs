using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;

namespace Nyan.Modules.Data.Oracle
{
    public class OracleAdapter : AdapterPrimitive
    {
        public OracleAdapter()
        {
            //SQLite implements regular ANSI SQL, so we don't to customize the base templates.

            parameterIdentifier = ":";
            useOutputParameterForInsertedKeyExtraction = true; //Some DBs may require an OUT parameter to extract the new ID. Not the case here.
            sqlTemplateInsertSingleWithReturn = "INSERT INTO {0} ({1}) VALUES ({2}) RETURNING CAST({3} AS VARCHAR2(38) ) INTO :newid";
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
                    MicroEntity<T>.QuerySingleValue<int>("SELECT COUNT(*) FROM ALL_TABLES WHERE table_name = '" + tn +
                                                          "'");

                if (tableCount != 0) return;

                Core.Settings.Current.Log.Add(typeof(T).FullName + ": Initializing schema");

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
                    var pDestinyType = "VARCHAR2(255)";
                    var pNullableSpec = "";
                    var pSourceName = prop.Name;

                    if (pType.IsPrimitiveType())
                    {
                        if (pType.IsArray) continue;
                        if (!(typeof(string) == pType) && typeof(IEnumerable).IsAssignableFrom(pType)) continue;
                        if (typeof(ICollection).IsAssignableFrom(pType)) continue;
                        if (typeof(IList).IsAssignableFrom(pType)) continue;
                        if (typeof(IDictionary).IsAssignableFrom(pType)) continue;

                        if (pType.BaseType != null &&
                            (typeof(IList).IsAssignableFrom(pType.BaseType) && pType.BaseType.IsGenericType)) continue;

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
                    Core.Settings.Current.Log.Add(typeof(T).FullName + ": Applying schema");
                    MicroEntity<T>.Execute(tableRender.ToString());
                }
                catch (Exception e)
                {
                    Core.Settings.Current.Log.Add(e);
                }

                if (MicroEntity<T>.Statements.IdColumn != null)
                {
                    try
                    {
                        MicroEntity<T>.Execute("DROP SEQUENCE " + seqName);
                    }
                    catch
                    {
                    }

                    try
                    {
                        Core.Settings.Current.Log.Add(typeof(T).FullName + ": Creating Sequence " + seqName);
                        MicroEntity<T>.Execute("CREATE SEQUENCE " + seqName);
                    }
                    catch (Exception e)
                    {
                        Core.Settings.Current.Log.Add(e);
                    }

                    //Primary Key
                    var pkStat = "ALTER TABLE " + tn + " ADD (CONSTRAINT " + tn + "_PK PRIMARY KEY (" +
                                 MicroEntity<T>.TableData.IdentifierColumnName + "))";

                    try
                    {
                        Core.Settings.Current.Log.Add(typeof(T).FullName + ": Adding Primary Key");
                        MicroEntity<T>.Execute(pkStat);
                    }
                    catch (Exception e)
                    {
                        Core.Settings.Current.Log.Add(e);
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
                    Core.Settings.Current.Log.Add(typeof(T).FullName + ": Adding BEFORE INSERT Trigger");
                    MicroEntity<T>.Execute(string.Format(trigStat,
                        MicroEntity<T>.Statements.SchemaElements["BeforeInsertTrigger"].Value, tn, seqName,
                        MicroEntity<T>.TableData.IdentifierColumnName));
                }
                catch (Exception e)
                {
                    Core.Settings.Current.Log.Add(e);
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
                    Core.Settings.Current.Log.Add(typeof(T).FullName + ": Adding BEFORE UPDATE Trigger");
                    MicroEntity<T>.Execute(string.Format(trigStat,
                        MicroEntity<T>.Statements.SchemaElements["BeforeUpdateTrigger"].Value, tn, seqName,
                        MicroEntity<T>.TableData.IdentifierColumnName));
                }
                catch (Exception e)
                {
                    Core.Settings.Current.Log.Add(e);
                }


                //Now, add comments to everything.

                var ocld = "|| CHAR(10) ||";
                var ocfld = "|| CHAR(10);";
                var commentStat =
                    "COMMENT ON TABLE " + tn + " IS 'Auto-generated table for Entity " + typeof(T).FullName + ".'" +
                    ocld +
                    "'Supporting structures:' " + ocld +
                    "'    Sequence: " + seqName + "'" + ocld +
                    "'    Triggers: " +
                    MicroEntity<T>.Statements.SchemaElements["BeforeInsertTrigger"].Value
                    + ", " +
                    MicroEntity<T>.Statements.SchemaElements["BeforeUpdateTrigger"].Value
                    + "'" + ocfld;

                try
                {
                    MicroEntity<T>.Execute(commentStat);
                }
                catch
                {
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
            return new OracleConnection(connectionString);
        }

        public override void RenderSchemaEntityNames<T>()
        {
            Core.Settings.Current.Log.Add(GetType().FullName + ": Rendering schema element names");

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