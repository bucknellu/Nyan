using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nyan.Core.Modules.Data.Connection
{
    public static class Factory
    {
        public static CredentialSetPrimitive GetCredentialSetPerConnectionBundle(ConnectionBundlePrimitive pConn, Type pPrefCredSetType = null)
        {
            CredentialSetPrimitive ret = new CredentialSetPrimitive();
            ret.CredentialCypherKeys = new Dictionary<string, string>();
            ret.AssociatedBundleType = pConn.GetType();

            List<Type> probeTypes = new List<Type>();
            List<CredentialSetPrimitive> creds = new List<CredentialSetPrimitive>();
            List<CredentialSetPrimitive> tmpCreds = new List<CredentialSetPrimitive>();

            if (pPrefCredSetType != null)
                probeTypes.Add(pPrefCredSetType);

            var scanModules = Management.GetClassesByInterface<CredentialSetPrimitive>();

            probeTypes = probeTypes.Concat(scanModules).ToList();

            // Create instances for all probed Credential types;
            foreach (var i in probeTypes)
            {
                var a = i.CreateInstance<CredentialSetPrimitive>();
                creds.Add(a);
            }

            // Filter Instances out, based on target Connection Bundle:


            foreach (var i in creds)
            {
                if (i.AssociatedBundleType.ToString() == ret.AssociatedBundleType.ToString())
                    tmpCreds.Add(i);
            }

            creds = tmpCreds;

            // now, compile all entries, using definition order;

            if (creds.Count > 0)
                Settings.Current.Log.Add("    Credential set(s) found for " + ret.AssociatedBundleType.ToString(), Log.Message.EContentType.Info);

            foreach (var i in creds)
            {
                Settings.Current.Log.Add("        " + i.GetType().ToString(), Log.Message.EContentType.MoreInfo);

                foreach (var ii in i.CredentialCypherKeys)
                {
                    if (!ret.CredentialCypherKeys.ContainsKey(ii.Key))
                        ret.CredentialCypherKeys[ii.Key] = ii.Value;
                }
            }

            return ret;
        }
    }
}
