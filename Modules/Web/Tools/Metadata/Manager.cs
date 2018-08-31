using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Nyan.Core.Assembly;
using Nyan.Core.Diagnostics;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
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

        public static JToken Get(Dictionary<string, object> payload = null) { return Instance.Composite(null, payload); }

        public Dictionary<string, object> GetDistinctKeys()
        {
            var dict = new Dictionary<string, object>();

            foreach (var mdp in Providers)
            {
                var bagContents = mdp.Value.Fetch(new MetadataProviderPrimitive.KeyBag());

                if (bagContents != null) dict = dict.Union(bagContents).ToDictionary(d => d.Key, d => d.Value);
            }

            return dict;
        }

        public static T? NullableValue<T>(string path, Dictionary<string, object> payload = null) where T : struct
        {
            var metaVals = Instance.Composite(null, payload);
            var val = metaVals.SelectToken(path);
            return val?.ToObject<T>();
        }

        public static T Value<T>(string path, Dictionary<string, object> payload = null) where T : class
        {
            var metaVals = Instance.Composite(null, payload);
            var val = metaVals.SelectToken(path);
            return val?.ToObject<T>();
        }

        public JToken Composite(string key = null, Dictionary<string, object> payload = null, string path = null)
        {
            var tmp = new JObject();

            foreach (var mdp in Providers)
                try
                {
                    var content = mdp.Value.Get(path, key, payload);
                    // Current.Log.Add($"{ThreadHelper.Uid} Composite {mdp.Key}: (path={path}, key={key}, payload={payload.ToJson()}): {content.ToJson()}", Message.EContentType.Info);
                    tmp.Merge(content);
                } catch (Exception e)
                {
                    // Current.Log.Add($"{ThreadHelper.Uid} Metadata manager > Composite: {mdp.Key} {key}", Message.EContentType.Warning);
                    Current.Log.Add(e);
                }

            JToken ret = null;

            if (path != null) ret = tmp.SelectToken("$." + path);

            return ret ?? tmp;
        }

        public void Set(string pPath, object pValue, string scope, string pKey = null, bool preventStorage = false, Dictionary<string, object> payload = null)
        {
            if (payload != null)
            {
                var keyBag = new MetadataProviderPrimitive.KeyBag {payload = payload, Key = pKey};
                Providers[scope].Put(pPath, pValue, keyBag, preventStorage);
            }
            else { Providers[scope].Put(pPath, pValue, pKey, preventStorage); }
        }

        public void Set(string pPath, object pValue, string scope, MetadataProviderPrimitive.KeyBag keyBag, bool preventStorage = false) { Providers[scope].Put(pPath, pValue, keyBag, preventStorage); }
    }
}