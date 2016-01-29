using System;
using System.Collections.Generic;

namespace Nyan.Core.Modules.Data.Connection
{
    public abstract class ConnectionBundlePrimitive
    {
        public Dictionary<string, string> ConnectionCypherKeys { get; set; }
        public Type AdapterType { get; set; }
        public virtual void ValidateDatabase() { }
    }
}