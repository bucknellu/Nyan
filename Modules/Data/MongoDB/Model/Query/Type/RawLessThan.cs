using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class RawLessThan : IRawMongoValue
    {
        [JsonProperty("$lt")]
        public JRaw Value { get; set; }
    }
}