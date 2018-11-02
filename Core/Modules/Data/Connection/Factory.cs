using System;
using System.Collections.Generic;
using System.Linq;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Data.Connection
{
    public static class Factory
    {

        private static readonly Dictionary<ConnectionBundlePrimitive, CredentialSetPrimitive> Cache = new Dictionary<ConnectionBundlePrimitive, CredentialSetPrimitive>();
        private static readonly object OLock = new object();


        public static CredentialSetPrimitive GetCredentialSetPerConnectionBundle(ConnectionBundlePrimitive pConn, Type pPrefCredSetType = null)
        {
            lock (OLock)
            {
                if (pPrefCredSetType == null)
                {
                    if (Cache.ContainsKey(pConn)) return Cache[pConn];
                }

                var ret = new CredentialSetPrimitive
                {
                    CredentialCypherKeys = new Dictionary<string, string>(),
                    AssociatedBundleType = pConn.GetType()
                };

                var probeTypes = new List<Type>();

                if (pPrefCredSetType != null)
                {
                    probeTypes.Add(pPrefCredSetType);
                }

                var scanModules = Management.GetClassesByInterface<CredentialSetPrimitive>();

                probeTypes = probeTypes.Concat(scanModules).ToList();

                // Create instances for all probed Credential types;
                var creds = probeTypes.Select(i => i.CreateInstance<CredentialSetPrimitive>()).ToList();

                // Filter Instances out, based on target Connection Bundle:

                var tmpCreds = creds.Where(i => i.AssociatedBundleType.ToString() == ret.AssociatedBundleType.ToString()).ToList();

                creds = tmpCreds;

                // now, compile all entries, using definition order;

                // if (creds.Count > 0) Current.Log.Add("[" + ret.AssociatedBundleType + "] Credential sets: " + string.Join(",", creds.Select(i => "[" + i.GetType().Name + "]")), Message.EContentType.Info);

                foreach (var i in creds)
                {
                    foreach (var ii in i.CredentialCypherKeys)
                    {
                        if (!ret.CredentialCypherKeys.ContainsKey(ii.Key))
                            ret.CredentialCypherKeys[ii.Key] = ii.Value;
                    }
                }

                if (pPrefCredSetType == null)
                    Cache[pConn] = ret;

                return ret;
            }
        }
    }
}