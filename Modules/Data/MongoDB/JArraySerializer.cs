using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Linq;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB
{
    public class JArraySerializer : SerializerBase<JArray>
    {
        public override JArray Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var myBSONDoc = BsonDocumentSerializer.Instance.Deserialize(context);

            var val = myBSONDoc["_c"];

            return JArray.Parse(val.ToString());
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JArray value)
        {
            var val = Serialization.ToJson(value);

            

                BsonDocumentSerializer.Instance.Serialize(context, BsonDocument.Parse("{_c:" + val + "}"));

            //var myBSONDoc = BsonDocument.Parse(value.ToString());
            //BsonDocumentSerializer.Instance.Serialize(context, myBSONDoc);
        }
    }
}