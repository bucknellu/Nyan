using System;
using System.Collections.Generic;

namespace Nyan.Core.Modules.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MicroEntitySetupAttribute : Attribute
    {
        /// <summary>
        ///     When set, specifies distinct encrypted username/passwords per environment.
        /// </summary>
        public Dictionary<string, string> CredentialCypherKeys = new Dictionary<string, string>();

        public string WebApiControllerName;
        /// <summary>
        ///     When set, allows a basic corresponding schema to be created on the database, based on the current class'
        ///     properties.
        /// </summary>
        public bool AutoGenerateMissingSchema { get; set; } = true;

        /// <summary>
        ///     When set, it supersedes the individual AdapterType and ConnectionCypherKey settings, including
        ///     environment-dependent values.
        /// </summary>
        public Type ConnectionBundleType { get; set; } = null;

        /// <summary>
        ///     Force columns to be remapped to oracle dynamic parameters each time the record is saved.
        ///     This is useful for columns that are likely to be lengthy varchars (> 2000 characters long).
        /// </summary>
        public bool ForceMapUse { get; set; } = false;

        public string IdentifierColumnName { get; set; }
        public string Label { get; set; }

        /// <summary>
        ///     Indicates whether import/export maintenance procedures should ignore physically stored data.
        /// </summary>
        public bool IgnoreDataOnMaintenance { get; set; } = false;

        /// <summary>
        ///     Indicates whether the record identifier is insertable or not.
        /// </summary>
        public bool IsInsertableIdentifier { get; set; } = false;

        /// <summary>
        ///     When set, marks the entity as read-only. Schema creation and change methods (Delete, Save and Insert) will fail if
        ///     called.
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        public string TableName { get; set; }
        public string TablePrefix { get; set; } = "";
        public bool UseCaching { get; set; } = true;
        public Type AdapterType { get; set; }
        public bool AuditAccess { get; set; } = false;
        public bool AuditChange { get; set; } = false;
        public Type CredentialSetType { get; set; } = null;
        public string PersistentEnvironmentCode { get; set; }
        public bool SuppressErrors { get; set; }
        public bool IgnoreEnvironmentPrefix { get; set; }
    }
}