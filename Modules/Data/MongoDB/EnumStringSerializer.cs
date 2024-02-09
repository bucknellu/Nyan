using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;

namespace Nyan.Modules.Data.MongoDB
{
    public class EnumStringSerializer<TEnum> : EnumSerializer<TEnum> where TEnum : struct, Enum
    {
        public EnumStringSerializer() : base(BsonType.String) { }
    }
}