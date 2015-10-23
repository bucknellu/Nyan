using System.Collections.Generic;
using Dapper;
using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace Nyan.Modules.Data.Oracle
{
    public class OracleDynamicParameters : DynamicParametersPrimitive, SqlMapper.IDynamicParameters
    {
        private List<object> _templates;

        public OracleDynamicParameters()
        {
        }
        public OracleDynamicParameters(object template)
        {
            AddDynamicParams(template);
        }
        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            AddParameters(command, identity);
        }

        public void AddDynamicParams(object param)
        {
            var obj = param;

            if (obj == null) return;

            ResetCachedWhereClause();

            var subDynamic = obj as OracleDynamicParameters;
            if (subDynamic == null)
            {
                var dictionary = obj as IEnumerable<KeyValuePair<string, object>>;
                if (dictionary == null)
                {
                    _templates = _templates ?? new List<object>();
                    _templates.Add(obj);
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

                if (subDynamic._templates != null)
                {
                    _templates = _templates ?? new List<object>();
                    foreach (var t in subDynamic._templates)
                        _templates.Add(t);
                }
            }
        }
        public override void Add(string name, object value = null, DbGenericType? dbType = null, ParameterDirection? direction = null, int? size = null)
        {
            if (value is bool)
                value = (bool)value ? 1 : 0;

            base.Add(name, value, dbType, direction, size);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static DbType ConvertGenericTypeToCustomType(DbGenericType type)
        {
            switch (type)
            {
                case DbGenericType.String:
                    return DbType.String;
                case DbGenericType.Fraction:
                    return DbType.Decimal;
                case DbGenericType.Number:
                    return DbType.Int64;
                case DbGenericType.Bool:
                    return DbType.Boolean;
                case DbGenericType.DateTime:
                    return DbType.DateTime;
                case DbGenericType.LargeObject:
                    return DbType.Object;
                default:
                    return DbType.String;
            }
        }
        public override ParameterInformation CustomizeParameterInformation(ParameterInformation p)
        {
            p.TargetDatabaseType = ConvertGenericTypeToCustomType(p.Type);
            return p;
        }
        public override void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            ResetCachedWhereClause();

            if (_templates != null)
            {
                foreach (var template in _templates)
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

                var add = !((OracleCommand)command).Parameters.Contains(name);

                OracleParameter p;

                if (add)
                {
                    p = ((OracleCommand)command).CreateParameter();
                    p.ParameterName = name;
                }
                else
                {
                    p = ((OracleCommand)command).Parameters[name];
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

                p.OracleDbType = (OracleDbType)param.Value.TargetDatabaseType;

                if (add)
                    command.Parameters.Add(p);

                param.Value.AttachedParameter = p;
            }
        }
    }
}