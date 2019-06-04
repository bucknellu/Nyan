using System.Collections;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;

namespace Nyan.Modules.Data.MongoDB
{
    // https://stackoverflow.com/a/51546410/1845714
    public class DictionarySerializer<TDictionary, TKeySerializer, TValueSerializer> : DictionarySerializerBase<TDictionary>
        where TDictionary : class, IDictionary, new()
        where TKeySerializer : IBsonSerializer, new()
        where TValueSerializer : IBsonSerializer, new()
    {
        public DictionarySerializer() : base(DictionaryRepresentation.Document, new TKeySerializer(), new TValueSerializer())
        { }

        protected override TDictionary CreateInstance()
        {
            return new TDictionary();
        }
    }
}