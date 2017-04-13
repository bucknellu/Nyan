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

namespace Nyan.Modules.Data.MongoDB
{
    public class MongoDbAdapter : DataAdapterPrimitive
    {
        public MongoDbAdapter()
        {
            useOutputParameterForInsertedKeyExtraction = true;
                //Some DBs may require an OUT parameter to extract the new ID. Not the case here.
            sqlTemplateInsertSingleWithReturn =
                "INSERT INTO {0} ({1}) VALUES ({2}) RETURNING CAST({3} AS VARCHAR2(38) ) INTO {4}newid";
            sqlTemplateTableTruncate = "TRUNCATE TABLE {0}";
            //No such thing as TRUNCATE on SQLite, but open DELETE works the same way.

            dynamicParameterType = typeof(MongoDbDynamicParameters);


            Interceptor = new MongoDbinterceptor(this);
        }

        public override void RenderSchemaEntityNames<T>() { }
        public override DbConnection Connection(string connectionString) { return null; }

        public override void CheckDatabaseEntities<T>() {}

    }
}