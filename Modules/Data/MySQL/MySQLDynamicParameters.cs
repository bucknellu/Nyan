using Dapper;
using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Data;

namespace Nyan.Modules.Data.MySQL
{
    public class MySQLDynamicParameters : DynamicParametersPrimitive, SqlMapper.IDynamicParameters
    {
        public override void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            throw new NotImplementedException();
        }
    }
}
