using System;
using System.Collections.Generic;

namespace Nyan.Core.Modules.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MicroEntitySetupAttribute : Attribute
    {
        public string WebApiControllerName;

        public MicroEntitySetupAttribute()
        {
            AuditChange = false;
            AuditAccess = false;
            IsReadOnly = false;
            TablePrefix = "";
            UseCaching = true;
            IsInsertableIdentifier = false;
            IgnoreDataOnMaintenance = false;
            ForceMapUse = false;
            ConnectionBundleType = null;
            CredentialSetType = null;
            AutoGenerateMissingSchema = true;
        }

        /// <summary>
        /// When set, allows a basic corresponding schema to be created on the database, based on the current class' properties.
        /// </summary>
        public bool AutoGenerateMissingSchema { get; set; }
        public string PersistentEnvironmentCode { get; set; }
        /// <summary>
        /// When set, it supersedes the individual AdapterType and ConnectionCypherKey settings, including environment-dependent values.
        /// </summary>
        public Type ConnectionBundleType { get; set; }
        /// <summary>
        /// When set, specifies distinct encrypted username/passwords per environment.
        /// </summary>
        public Dictionary<string, string> CredentialCypherKeys = new Dictionary<string, string>();
        /// <summary>
        /// Force columns to be remapped to oracle dynamic parameters each time the record is saved. 
        /// This is useful for columns that are likely to be lengthy varchars (> 2000 characters long).
        /// </summary>
        public bool ForceMapUse { get; set; }
        public string IdentifierColumnName { get; set; }
        public string Label { get; set; }
        /// <summary>
        /// Indicates whether import/export maintenance procedures should ignore physically stored data.
        /// </summary>
        public bool IgnoreDataOnMaintenance { get; set; }
        /// <summary>
        /// Indicates whether the record identifier is insertable or not.
        /// </summary>
        public bool IsInsertableIdentifier { get; set; }
        /// <summary>
        /// When set, marks the entity as read-only. Schema creation and change methods (Delete, Save and Insert) will fail if called.
        /// </summary>
        public bool IsReadOnly { get; set; }
        public string TableName { get; set; }
        public string TablePrefix { get; set; }
        public bool UseCaching { get; set; }
        public Type AdapterType { get; set; }
        public bool AuditAccess { get; set; }
        public bool AuditChange { get; set; }
        public Type CredentialSetType { get; set; }
    }
}