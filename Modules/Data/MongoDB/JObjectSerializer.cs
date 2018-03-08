using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Nyan.Modules.Data.MongoDB {
    public class JObjectSerializer : SerializerBase<Newtonsoft.Json.Linq.JObject>
    {
        public override Newtonsoft.Json.Linq.JObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var myBSONDoc = BsonDocumentSerializer.Instance.Deserialize(context);
            return Newtonsoft.Json.Linq.JObject.Parse(myBSONDoc.ToString());
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Newtonsoft.Json.Linq.JObject value)
        {
            var myBSONDoc = BsonDocument.Parse(value.ToString());
            BsonDocumentSerializer.Instance.Serialize(context, myBSONDoc);
        }
    }
}