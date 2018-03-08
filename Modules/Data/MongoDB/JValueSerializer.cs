using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB
{
    public class JValueSerializer : SerializerBase<JValue>
    {
        public override JValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var myBSONDoc = BsonDocumentSerializer.Instance.Deserialize(context);
            return new JValue(myBSONDoc.ToString());
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JValue value)
        {
            var myBSONDoc = BsonDocument.Parse(value.ToString());
            BsonDocumentSerializer.Instance.Serialize(context, myBSONDoc);
        }
    }
}