using Dapper;
using MySql.Data.MySqlClient;
using Nyan.Core.Modules.Data.Adapter;
using System.Data;

namespace Nyan.Modules.Data.MySql
{
    public class MySqlDynamicParameters : DynamicParametersPrimitive, SqlMapper.IDynamicParameters
    {
        public MySqlDynamicParameters()
        {
            CommandType = typeof(MySqlCommand);
            ParameterType = typeof(MySqlParameter);
        }

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
    }
}
