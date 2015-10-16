using System;
using System.Linq;
using Dapper;

namespace Nyan.Core.Modules.Data
{
    public class ColumnAttributeTypeMapper<T> : FallBackTypeMapper
    {
        public ColumnAttributeTypeMapper()
            : base(new SqlMapper.ITypeMap[]
            {
                new CustomPropertyTypeMap(
                    typeof (T),
                    (type, columnName) =>
                        type.GetProperties()
                            .FirstOrDefault(prop =>
                                prop.GetCustomAttributes(false)
                                    .OfType<ColumnAttribute>()
                                    .Any(attr => string.Equals(attr.Name, columnName, StringComparison.OrdinalIgnoreCase))
                            )
                    ),
                new DefaultTypeMap(typeof (T))
            })
        { }
    }
}
