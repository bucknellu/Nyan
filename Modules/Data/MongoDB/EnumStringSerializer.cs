using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;

namespace Nyan.Modules.Data.MongoDB
{
    public class EnumStringSerializer<TEnum> : EnumSerializer<TEnum> where TEnum : struct
    {
        public EnumStringSerializer() : base(BsonType.String) { }
    }
}