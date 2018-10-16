using System;
using Newtonsoft.Json;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class LessThanOrEqual
    {
        public LessThanOrEqual() { }
        public LessThanOrEqual(DateTime value) { Value = value.ToISODateString(); }
        public LessThanOrEqual(string value) { Value = value; }

        [JsonProperty("$lte")]
        public string Value { get; set; }
    }
}