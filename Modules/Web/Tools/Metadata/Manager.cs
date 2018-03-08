using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Settings;

// ReSharper disable InconsistentNaming

namespace Nyan.Modules.Web.Tools.Metadata
{
    public class Manager
    {
        public static Dictionary<string, MetadataProviderPrimitive> Providers = new Dictionary<string, MetadataProviderPrimitive>();

        public static Manager Instance = new Manager();

        static Manager()
        {
            var _providers = Management.GetClassesByInterface<MetadataProviderPrimitive>();
            _providers.Reverse();

            foreach (var i in _providers)
            {
                var instance = i.CreateInstance<MetadataProviderPrimitive>();
                Providers.Add(instance.Code, instance);
            }

            // Initialization
            foreach (var mtpp in Providers) mtpp.Value.Bootstrap();
        }

        public JToken Composite(string key = null, Dictionary<string, object> payload = null, string path = null)
        {
            var tmp = new JObject();

            foreach (var mdp in Providers)
                try
                {
                    tmp.Merge(mdp.Value.Get(path, key, payload));
                }
                catch (Exception e)
                {
                    Current.Log.Add(e, $"Metadata manager > Composite: {mdp.Key} {key}");
                }

            JToken ret = null;

            if (path != null) ret = tmp.SelectToken("$." + path);

            return ret ?? tmp;
        }

        public void Set(string pPath, object pValue, string scope, string pKey = null, bool preventStorage = false, Dictionary<string, object> payload = null)
        {
            var keyBag = new MetadataProviderPrimitive.KeyBag { payload = payload, Key = pKey };
            Providers[scope].Put(pPath, pValue, keyBag, preventStorage);
        }
        public void Set(string pPath, object pValue, string scope, MetadataProviderPrimitive.KeyBag keyBag, bool preventStorage = false)
        {
            Providers[scope].Put(pPath, pValue, keyBag, preventStorage);
        }
    }
}