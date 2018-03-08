using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class RawGreaterThan : IRawMongoValue
    {
        [JsonProperty("$gt")]
        public JRaw Value { get; set; }
    }
}