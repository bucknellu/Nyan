using Newtonsoft.Json;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class LessThan
    {
        [JsonProperty("$lt")]
        public string Value { get; set; }
    }
}