using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Nyan.Core.Extensions;

namespace Nyan.Core.Modules.Data.Adapter
{
    public abstract class DynamicParametersPrimitive : SqlMapper.IDynamicParameters
    {
        public enum DbGenericType
        {
            Bool,
            DateTime,
            Fraction,
            Number,
            OutCursor,
            String,
            LargeObject
        }

        private static readonly Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> _paramReaderCache =
            new Dictionary<SqlMapper.Identity, Action<IDbCommand, object>>();

        private readonly Dictionary<string, ParameterInformation> _internalParameters =
            new Dictionary<string, ParameterInformation>();

        private string _sqlInClause;
        private string _sqlWhereClause;

        protected DynamicParametersPrimitive()
        {
        }

        protected DynamicParametersPrimitive(object template)
        {
        }

        public virtual IEnumerable<string> ParameterNames
        {
            get { return _internalParameters.Select(p => p.Key); }
        }

        //Generic WHERE clause render.
        public virtual string SqlWhereClause
        {
            get
            {
                if (_sqlWhereClause != null) return _sqlWhereClause;

                _sqlWhereClause = "";

                foreach (var parameter in _internalParameters)
                {
                    if (_sqlWhereClause != "") _sqlWhereClause += " AND ";
                    _sqlWhereClause += parameter.Key + " = " + parameter.Value.Name;
                }

                return _sqlWhereClause;
            }
        }

        //Generic IN clause render.
        public virtual string SqlInClause
        {
            get
            {
                if (_sqlInClause != null) return _sqlInClause;

                _sqlInClause = "";

                foreach (var parameter in _internalParameters)
                {
                    if (_sqlInClause != "") _sqlInClause += ", ";
                    _sqlInClause += parameter.Value.Name;
                }

                return _sqlInClause;
            }
        }

        public virtual string ParameterTemplate
        {
            get
            {
                //Vanilla response. No transformation.
                return "{0}";
            }
        }

        public Dictionary<string, ParameterInformation> Parameters
        {
            get { return _internalParameters; }
        }

        public static Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> ParamReaderCache
        {
            get { return _paramReaderCache; }
        }

        public abstract void AddParameters(IDbCommand command, SqlMapper.Identity identity);

        public void ResetCachedWhereClause()
        {
            _sqlWhereClause = null;
        }

        public void ResetCachedInClause()
        {
            _sqlInClause = null;
        }

        public virtual void Add(string name, object value = null, DbGenericType? dbType = DbGenericType.String,
            ParameterDirection? direction = ParameterDirection.Input, int? size = null)
        {
            _sqlWhereClause = null; // Always reset WHERE clause.
            _sqlInClause = null; // Always reset IN clause.

            var ret = CustomizeParameterInformation(new ParameterInformation
            {
                Name = ParameterTemplate.format(name),
                Value = value,
                ParameterDirection = direction ?? ParameterDirection.Input, // No direction? Input then.
                Type = dbType ?? DbGenericType.LargeObject,
                // If no type is defined, it defaults to BLOB-like structures.
                Size = size
            });

            _internalParameters[name] = ret;
        }

        public virtual ParameterInformation CustomizeParameterInformation(ParameterInformation parameterInformation)
        {
            //Nothing to do really in this case. Inheriting classes may want to do something with it, though, like adding the target database type.
            return parameterInformation;
        }

        public virtual T Get<T>(string name)
        {
            var val = Parameters[name].AttachedParameter.Value;
            if (val != DBNull.Value) return (T)val;
            if (default(T) == null) return default(T);

            throw new ApplicationException("Attempting to cast a DBNull to a non nullable type!");
        }

        public override string ToString()
        {
            var ret = "No parameters listed";
            if (_internalParameters.Count == 0) return ret;

            ret = "";
            foreach (var parameter in _internalParameters)
            {
                if (ret != "") ret += ", ";
                ret += parameter.Value.Name + ":" + parameter.Value.Value;
            }

            return ret;
        }

        public class ParameterInformation
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public DbGenericType Type { get; set; }
            public ParameterDirection ParameterDirection { get; set; }
            public int? Size { get; set; }
            public virtual object TargetDatabaseType { get; set; }
            public IDbDataParameter AttachedParameter { get; set; }
        }
    }
}
