using System;
using System.Collections.Generic;

namespace Nyan.Core.Modules.Data.Connection
{
    public class CredentialSetPrimitive
    {
        public Type AssociatedBundleType { get; set; }
        public Dictionary<string, string> CredentialCypherKeys { get; set; }
    }
}