using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB
{
    public class JObjectMapper : ICustomBsonTypeMapper
    {
        public bool TryMapToBsonValue(object value, out BsonValue bsonValue)
        {
            var src = value as JObject;

            bsonValue = src != null ? BsonDocument.Parse("{prop0:" + src + "}")[0] : BsonValue.Create(value);

            return true;
        }
    }
}