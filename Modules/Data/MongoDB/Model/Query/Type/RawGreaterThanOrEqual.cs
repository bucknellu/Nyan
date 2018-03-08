using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class RawGreaterThanOrEqual : IRawMongoValue
    {
        [JsonProperty("$gte")]
        public JRaw Value { get; set; }
    }
}