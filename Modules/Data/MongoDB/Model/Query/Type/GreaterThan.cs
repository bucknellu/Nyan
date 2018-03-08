using Newtonsoft.Json;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class GreaterThan
    {
        [JsonProperty("$gt")]
        public string Value { get; set; }
    }
}