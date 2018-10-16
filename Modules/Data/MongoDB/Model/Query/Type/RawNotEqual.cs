using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class RawNotEqual : IRawMongoValue
    {
        public RawNotEqual() { }
        public RawNotEqual(string value) { Value = new JRaw(value); }
        [JsonProperty("$ne")]
        public JRaw Value { get; set; }
    }
}