using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Data.Connection
{
    public class CredentialSetPrimitive
    {
        public Type AssociatedBundleType { get; set; }
        public Dictionary<string, string> CredentialCypherKeys { get; set; }
    }
}
