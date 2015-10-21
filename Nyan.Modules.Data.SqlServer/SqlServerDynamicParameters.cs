using System;
using System.Data;
using Dapper;
using Nyan.Core.Modules.Data.Adapter;

namespace Nyan.Modules.Data.SqlServer
{
    public class SqlServerDynamicParameters : DynamicParametersPrimitive, SqlMapper.IDynamicParameters
    {
        public override void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            throw new NotImplementedException();
        }
    }
}
