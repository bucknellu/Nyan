using System;
using Newtonsoft.Json;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type {
    public class GreaterThanOrEqual
    {
        public GreaterThanOrEqual() { }
        public GreaterThanOrEqual(DateTime value) { Value = value.ToISODateString(); }
        public GreaterThanOrEqual(string value) { Value = value; }

        [JsonProperty("$gte")]
        public string Value { get; set; }
    }
}