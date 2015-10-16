using Dapper;
using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Data;

namespace Nyan.Modules.Data.Oracle
{
    public class OracleDynamicParameters : DynamicParametersPrimitive, SqlMapper.IDynamicParameters
    {
        public override void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            throw new NotImplementedException();
        }
    }
}
