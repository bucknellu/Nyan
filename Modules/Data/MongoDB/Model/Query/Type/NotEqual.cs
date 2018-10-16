using Newtonsoft.Json;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class NotEqual
    {
        public NotEqual() { }

        public NotEqual(string value) => Value = value;

        [JsonProperty("$ne")]
        public string Value { get; set; }
    }
}