using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using System.Data.Common;
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

        private static readonly Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> _paramReaderCache = new Dictionary<SqlMapper.Identity, Action<IDbCommand, object>>();
        private readonly Dictionary<string, ParameterInformation> _internalParameters = new Dictionary<string, ParameterInformation>();

        protected internal Type CommandType;
        protected internal Type ParameterType;

        protected internal string ParameterIdentifier = "@";

        public List<object> Templates;

        private string _sqlInClause;
        private string _sqlWhereClause;

        public virtual void AddDynamicParams(object param)
        {
            var obj = param;

            if (obj == null) return;

            ResetCachedWhereClause();

            var subDynamic = (DynamicParametersPrimitive)Activator.CreateInstance(GetType(), param);
            if (subDynamic == null)
            {
                var dictionary = obj as IEnumerable<KeyValuePair<string, object>>;
                if (dictionary == null)
                {
                    Templates = Templates ?? new List<object>();
                    Templates.Add(obj);
                }
                else
                {
                    foreach (var kvp in dictionary)
                        Add(kvp.Key, kvp.Value);
                }
            }
            else
            {
                if (subDynamic.Parameters != null)
                {
                    foreach (var kvp in subDynamic.Parameters)
                        Parameters.Add(kvp.Key, kvp.Value);
                }

                if (subDynamic.Templates != null)
                {
                    Templates = Templates ?? new List<object>();
                    foreach (var t in subDynamic.Templates)
                        Templates.Add(t);
                }
            }
        }

        public DynamicParametersPrimitive()
        {
        }
        public DynamicParametersPrimitive(object template)
        {
            AddDynamicParams(template);
        }
        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            AddParameters(command, identity);
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
                    _sqlWhereClause += parameter.Key + " = " + ParameterIdentifier + parameter.Value.Name;
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

        public Dictionary<string, ParameterInformation> Parameters
        {
            get { return _internalParameters; }
        }

        public static Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> ParamReaderCache
        {
            get { return _paramReaderCache; }
        }

        public virtual void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            ResetCachedWhereClause();

            if (Templates != null)
            {
                foreach (var template in Templates)
                {
                    var newIdent = identity.ForDynamicParameters(template.GetType());

                    Action<IDbCommand, object> appender;

                    lock (ParamReaderCache)
                    {
                        if (!ParamReaderCache.TryGetValue(newIdent, out appender))
                        {
                            appender = SqlMapper.CreateParamInfoGenerator(newIdent, false, false);
                            ParamReaderCache[newIdent] = appender;
                        }
                    }

                    appender(command, template);
                }
            }

            foreach (var param in Parameters)
            {
                var name = param.Key;

                dynamic dCommand = Convert.ChangeType(command, CommandType);

                var add = !dCommand.Parameters.Contains(name);

                var p = (DbParameter)Activator.CreateInstance(ParameterType);

                if (add)
                {
                    p = dCommand.CreateParameter();
                    p.ParameterName = name;
                }
                else
                {
                    p = dCommand.Parameters[name];
                }
                var val = param.Value.Value;

                p.Value = val ?? DBNull.Value;
                p.Direction = param.Value.ParameterDirection;

                var s = val as string;

                if (s != null)
                {
                    if (s.Length <= 4000)
                        p.Size = 4000;
                }
                if (param.Value.Size != null)
                {
                    p.Size = param.Value.Size.Value;
                }

                p.DbType = (DbType)param.Value.TargetDatabaseType;

                if (add)
                    command.Parameters.Add(p);

                param.Value.AttachedParameter = p;
            }
        }

        public void ResetCachedWhereClause()
        {
            _sqlWhereClause = null;
        }

        public void ResetCachedInClause()
        {
            _sqlInClause = null;
        }

        public virtual void Add(string name, object value = null, DbGenericType? dbType = DbGenericType.String, ParameterDirection? direction = ParameterDirection.Input, int? size = null)
        {
            _sqlWhereClause = null; // Always reset WHERE clause.
            _sqlInClause = null; // Always reset IN clause.

            if (dbType == null)
            {
                if (value.IsNumeric())
                    dbType = DbGenericType.Number;
            }

            var ret = CustomizeParameterInformation(new ParameterInformation
            {
                Name = name,
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