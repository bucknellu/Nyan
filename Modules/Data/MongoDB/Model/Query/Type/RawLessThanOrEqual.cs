using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class RawLessThanOrEqual : IRawMongoValue
    {
        [JsonProperty("$lte")]
        public JRaw Value { get; set; }
    }
}