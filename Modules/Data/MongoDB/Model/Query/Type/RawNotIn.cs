using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class RawNotIn : IRawMongoValue
    {
        public RawNotIn() { }
        public RawNotIn(string value) { Value = new JRaw(value); }
        [JsonProperty("$nin")]
        public JRaw Value { get; set; }
    }
}