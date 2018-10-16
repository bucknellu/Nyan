using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class RawBetweenEqualFirst : IRawMongoValue
    {
        public RawBetweenEqualFirst() { }

        public RawBetweenEqualFirst(DateTime value, DateTime value2)
        {
            Value = new JRaw(value.ToISODateString());
            Value2 = new JRaw(value2.ToISODateString());
        }

        [JsonProperty("$lt")]
        public JRaw Value2 { get; set; }

        [JsonProperty("$gte")]
        public JRaw Value { get; set; }
    }
}