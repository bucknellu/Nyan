using Newtonsoft.Json;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class LessThanOrEqual
    {
        [JsonProperty("$lte")]
        public string Value { get; set; }
    }
}