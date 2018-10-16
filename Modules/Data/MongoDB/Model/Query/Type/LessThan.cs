using System;
using Newtonsoft.Json;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public class LessThan
    {
        public LessThan() { }
        public LessThan(DateTime value) { Value = value.ToISODateString(); }
        public LessThan(string value) { Value = value; }
        [JsonProperty("$lt")]
        public string Value { get; set; }
    }
}