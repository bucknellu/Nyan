using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class RawBetweenEqual : IRawMongoValue
    {
        public RawBetweenEqual() { }

        public RawBetweenEqual(DateTime value, DateTime value2)
        {
            Value = new JRaw(value.ToISODateString(true));
            Value2 = new JRaw(value2.ToISODateString(true));
        }

        [JsonProperty("$lte")]
        public JRaw Value2 { get; set; }

        [JsonProperty("$gte")]
        public JRaw Value { get; set; }
    }
}