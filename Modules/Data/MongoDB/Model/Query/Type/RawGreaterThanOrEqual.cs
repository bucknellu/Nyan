using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class RawGreaterThanOrEqual : IRawMongoValue
    {
        public RawGreaterThanOrEqual() { }
        public RawGreaterThanOrEqual(DateTime value) { Value = new JRaw(value.ToISODateString()); }

        [JsonProperty("$gte")]
        public JRaw Value { get; set; }
    }
}