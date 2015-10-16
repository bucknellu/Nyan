using System;
using System.Collections.Generic;

namespace Nyan.Core.Modules.Data.Connection
{
    public abstract class BundlePrimitive
    {
        public Dictionary<string, string> EnvironmentCypherKeys { get; set; }
        public Type AdapterType { get; set; }

        public abstract void ValidateDatabase();
    }
}