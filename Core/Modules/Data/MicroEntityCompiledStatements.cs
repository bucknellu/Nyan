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
            CriticalFailure,
            ShuttingDown
        }

        public class MicroEntityState
        {
            public EStatus Status { get; set; }
            protected internal string Description { get; internal set; }
            protected internal string Step { get; internal set; }
            protected internal string Stack { get; internal set; }
            public MicroEntityState() { Status = EStatus.Undefined; }
        }

        protected internal DataAdapterPrimitive Adapter;
        protected internal ConnectionBundlePrimitive Bundle;
        protected internal CredentialSetPrimitive CredentialSet;

        public IInterceptor Interceptor;

        public Dictionary<string, string> ConnectionCypherKeys = new Dictionary<string, string>();
        public Dictionary<string, string> CredentialCypherKeys = new Dictionary<string, string>();

        public string ConnectionString;
        public string CredentialsString;
        public string IdColumn;
        public string Label;
        public string IdProperty;
        public string IdPropertyRaw;
        public DateTime PrdConfigLastChange;
        public Dictionary<string, string> PropertyFieldMap;
        public Dictionary<string, long> PropertyLengthMap;
        public Dictionary<string, bool> PropertySerializationMap;
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

        public string SqlSimpleQueryTerm;

        public string SqlGetAll;
        public string SqlGetSingle;

        public string SqlInsertSingle;
        public string SqlInsertSingleWithReturn;
        public string SqlReturnNewIdentifier;
        public string SqlRemoveSingleParametrized;
        public string SqlTruncateTable;
        public string SqlUpdateSingle;
        public string SqlOrderByCommand;
        public string SqlPaginationWrapper;

        public MicroEntityState State = new MicroEntityState();

        public string EnvironmentCode;
        public string SqlRowCount;
    }
}