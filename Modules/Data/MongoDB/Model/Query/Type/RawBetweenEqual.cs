using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class RawBetweenEqual : IRawMongoValue
    {
        [JsonProperty("$gte")]
        public JRaw Value { get; set; }
        [JsonProperty("$lte")]
        public JRaw Value2 { get; set; }
    }
}