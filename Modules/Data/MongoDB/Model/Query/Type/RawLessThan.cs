using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class RawLessThan : IRawMongoValue
    {
        public RawLessThan() { }
        public RawLessThan(DateTime value) { Value = new JRaw(value.ToISODateString()); }
        [JsonProperty("$lt")]
        public JRaw Value { get; set; }
    }
}