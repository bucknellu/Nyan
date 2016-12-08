using System.Data;
using Nyan.Core.Modules.Data.Adapter;

namespace Nyan.Modules.Data.MongoDB
{
    public class MongoDbDynamicParameters : DynamicParametersPrimitive
    {
        public MongoDbDynamicParameters()
        {
            // CommandType = typeof(OracleCommand);
            // ParameterType = typeof(OracleParameter);

            ParameterDefinition.Identifier = ":";
        }

        public override void Add(string name, object value = null, DbGenericType? dbType = null, ParameterDirection? direction = null, int? size = null)
        {
            if (value is bool) value = (bool)value ? 1 : 0; //Oracle doesn't like BOOL.

            base.Add(name, value, dbType, direction, size);
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
                    return DbType.Int16; //Silly, I know, but Oracle doesn't support Boolean types.
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