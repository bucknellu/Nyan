using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class RawLessThanOrEqual : IRawMongoValue
    {
        public RawLessThanOrEqual() { }
        public RawLessThanOrEqual(DateTime value) { Value = new JRaw(value.ToISODateString()); }
        [JsonProperty("$lte")]
        public JRaw Value { get; set; }
    }
}