using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class RawGreaterThan : IRawMongoValue
    {
        public RawGreaterThan() { }
        public RawGreaterThan(DateTime value) { Value = new JRaw(value.ToISODateString()); }
        [JsonProperty("$gt")]
        public JRaw Value { get; set; }
    }
}