using System;
using System.Collections.Generic;
using Nyan.Core.Modules.Data.Adapter;
using Nyan.Core.Modules.Data.Connection;

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

        protected internal DataAdapterPrimitive Adapter;
        protected internal ConnectionBundlePrimitive Bundle;

        public Dictionary<string, string> ConnectionCypherKeys = new Dictionary<string, string>();
        public Dictionary<string, string> CredentialCypherKeys = new Dictionary<string, string>();

        public string ConnectionString;
        public string IdColumn;
        public string Label;
        public string IdProperty;
        public string IdPropertyRaw;
        public DateTime PrdConfigLastChange;
        public Dictionary<string, string> PropertyFieldMap;
        public Dictionary<string, KeyValuePair<string, string>> SchemaElements;

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
        public string SqlReturnNewIdentifier;
        public string SqlRemoveSingleParametrized;
        public string SqlTruncateTable;
        public string SqlUpdateSingle;

        protected internal EStatus Status { get; internal set; }
        protected internal string StatusDescription { get; internal set; }
        protected internal string StatusStep { get; internal set; }
    }
}