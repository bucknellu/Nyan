using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB
{
    public class JArraySerializer : SerializerBase<JArray>
    {
        public override JArray Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var myBSONDoc = BsonArraySerializer.Instance.Deserialize(context);
            return JArray.Parse(myBSONDoc.ToString());
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JArray value)
        {
            var jsonDoc = JsonConvert.SerializeObject(value);
            var bsonDoc = BsonSerializer.Deserialize<BsonArray>(jsonDoc);
            BsonArraySerializer.Instance.Serialize(context, bsonDoc);
        }
    }
}