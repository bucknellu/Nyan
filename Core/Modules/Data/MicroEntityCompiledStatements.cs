using Nyan.Core.Modules.Data.Adapter;
using Nyan.Core.Modules.Data.Connection;
using System;
using System.Collections.Generic;

namespace Nyan.Core.Modules.Data
{
    public class MicroEntityCompiledStatements
    {
        public enum EStatus
        {
            Undefined,
            Initializing,
            Operational,
            RecoverableFailure,
            CriticalFailure
        }

        protected internal AdapterPrimitive Adapter;
        protected internal BundlePrimitive Bundle;

        public string ConnectionString;
        public string IdColumn;
        public string IdProperty;
        public string IdPropertyRaw;
        public DateTime PrdConfigLastChange;
        public Dictionary<string, string> PropertyFieldMap;
        public Dictionary<string, KeyValuePair<string, string>> SchemaElements;
        public Dictionary<string, string> ConnectionCypherKeys = new Dictionary<string, string>();

        /// <summary>
        ///     The SQL query template for queries returning all fields. 
        ///     Format: 
        ///         SELECT * FROM [UserName.][TableName] WHERE ({0})
        /// </summary>
        public string SqlAllFieldsQueryTemplate;

        /// <summary>
        ///     The SQL query template for queries with custom SELECT clause. 
        ///     Format: 
        ///         SELECT {0} FROM [UserName.][TableName] WHERE ({1})
        /// </summary>
        public string SqlCustomSelectQueryTemplate;

        public string SqlGetAll;
        public string SqlGetSingle;

        public string SqlInsertSingle;
        public string SqlInsertSingleWithReturn;
        public string SqlRemoveSingle;
        public string SqlRemoveSingleParametrized;
        public string SqlUpdateSingle;
        protected internal EStatus Status { get; internal set; }
        protected internal string StatusDescription { get; internal set; }
        protected internal string StatusStep { get; internal set; }
    }
}