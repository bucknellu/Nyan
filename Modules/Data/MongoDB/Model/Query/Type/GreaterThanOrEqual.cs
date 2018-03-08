using Newtonsoft.Json;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class GreaterThanOrEqual
    {
        [JsonProperty("$gte")]
        public string Value { get; set; }
    }
}