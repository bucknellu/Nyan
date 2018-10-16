using System;
using Newtonsoft.Json;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class GreaterThan
    {
        public GreaterThan() { }
        public GreaterThan(DateTime value) { Value = value.ToISODateString(); }
        public GreaterThan(string value) { Value = value; }
        [JsonProperty("$gt")]
        public string Value { get; set; }
    }
}