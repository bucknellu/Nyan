using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.Tools.Metadata
{
    public class MetadataProviderPrimitive
    {
        public int CacheTimespan { get; set; } = 180; // 3 mins

        public virtual void Bootstrap() { }

        public class ContextKeyBag
        {
            private readonly KeyBag _keyBag;
            private readonly MetadataProviderPrimitive _provider;

            public ContextKeyBag(KeyBag keyBag, MetadataProviderPrimitive metadataProviderPrimitive)
            {
                _keyBag = keyBag;
                _provider = metadataProviderPrimitive;
            }

            public bool IsValid => DefaultKey != null;

            public string DefaultKey
            {
                get
                {
                    var key = GetKeyValue;
                    if (key == null) return null;

                    return _provider.Code + ":" + key;
                }
            }

            public string CacheKey
            {
                get
                {
                    var cacheKey = "stack.metadata:" + DefaultKey;
                    return cacheKey;
                }
            }


            public string GetKeyValue
            {
                get
                {
                    if (_keyBag.Key != null) return _keyBag.Key;

                    if (_keyBag.payload != null) if (_keyBag.payload.ContainsKey(_provider.Code)) return _keyBag.payload[_provider.Code];

                    try {
                        return _provider.ContextLocator;
                    }
                    catch {
                        return null;
                    }
                }
            }
        }

        public class KeyBag
        {
            public string Key { get; set; }
            public Dictionary<string, string> payload { get; set; }

            public ContextKeyBag this[MetadataProviderPrimitive metadataProviderPrimitive]
            {
                get { return new ContextKeyBag(this, metadataProviderPrimitive); }
            }
        }

        #region Key management

        public virtual string ContextLocator => null;

        public virtual string Code { get; set; }

        #endregion

        #region Caching

        internal void StoreCache(JObject content, KeyBag keyBag)
        {
            var k = keyBag[this].DefaultKey;
            if (k == null) return;

            Current.Cache[keyBag[this].CacheKey, null, CacheTimespan] = content.ToJson();
        }

        internal JObject RetrieveCache(KeyBag keyBag)
        {
            var k = keyBag[this].DefaultKey;
            if (k == null) return new JObject();

            var tmp = Current.Cache[keyBag[this].CacheKey];

            if (tmp != null) return JObject.Parse(tmp);

            RebuildFromFetch(keyBag);

            tmp = Current.Cache[keyBag[this].CacheKey];
            return JObject.Parse(tmp);
        }

        private void RebuildFromFetch(KeyBag keyBag)
        {

            var fetch = Fetch(keyBag);
            var init = JObject.Parse("{}");

            foreach (var i in fetch)
                SetJObjectKey(ref init, i.Key, i.Value);

            StoreCache(init, keyBag); // Seeding cache
        }

        private void HandleCache(object pValue, string pPath, KeyBag keyBag)
        {
            var refJObject = RetrieveCache(keyBag);
            SetJObjectKey(ref refJObject, pPath, pValue);
            StoreCache(refJObject, keyBag);
        }

        private static void SetJObjectKey(ref JObject refJObject, string pPath, object pValue)
        {
            // http://stackoverflow.com/a/17529388/1845714

            var val2 = JToken.FromObject(pValue);

            var token = refJObject.SelectToken(pPath) as JValue;

            if (token == null)
            {
                dynamic jpart = refJObject;

                foreach (var part in pPath.Split('.'))
                {
                    if (jpart[part] == null) jpart.Add(new JProperty(part, new JObject()));

                    jpart = jpart[part];
                }

                jpart.Replace(val2);
            }
            else token.Replace(val2);
        }

        #endregion

        #region Object handlers

        public JObject Get(string path = null, string key = null, Dictionary<string, string> payload = null)
        {
            var keyBag = new KeyBag
            {
                payload = payload,
                Key = key
            };

            return Get(path, keyBag);
        }

        public JObject Get(string path, KeyBag keyBag)
        {
            if (!keyBag[this].IsValid) return JObject.Parse("{}");

            var cacheKey = keyBag[this].CacheKey;

            // Put("context." + Code, keyBag.GetKeyValue(key, payload), key, true, payload);

            if (Current.Cache.OperationalStatus == EOperationalStatus.Operational)
            {
                var tmp = Current.Cache[cacheKey];

                if (tmp != null) return tmp.FromJson<JObject>();

                Current.Log.Add(cacheKey + " NOT FOUND", Message.EContentType.MoreInfo);
            }

            Fetch(keyBag);
            return RetrieveCache(keyBag);
        }

        public virtual void Put(string pPath, object pValue, string pKey = null, bool preventStorage = false,
            Dictionary<string, string> payload = null)
        {
            var keyBag = new KeyBag
            {
                payload = payload,
                Key = pKey
            };

            Put(pPath, pValue, keyBag, preventStorage);
        }

        public virtual void Put(string pPath, object pValue, KeyBag keyBag, bool preventStorage = false)
        {
            try
            {
                pPath = pPath.Trim().ToLower();

                if (!preventStorage) if (!Store(pPath, pValue, keyBag)) return;

                var dk = keyBag[this].DefaultKey;

                if (dk == null)
                {
                    Current.Log.Add("WARN: PUT INVALID: NOKEY [" + Code + "]" + pPath + ":" + pValue,
                        Message.EContentType.Maintenance);
                    return;
                }

                RebuildFromFetch(keyBag);

                //var cacheKey = CacheKey(pKey, payload);
                //Current.Cache.Remove(cacheKey);
            }
            catch (Exception e) {
                Current.Log.Add(e, "Metadata: [{0} - {1}] {2}".format(Code, pPath, pValue, pPath));
            }
        }

        #endregion

        #region Physical storage

        public virtual Dictionary<string, object> Fetch(KeyBag keyBag) { return new Dictionary<string, object>(); }

        public virtual bool Store(string pPath, object pValue, KeyBag keyBag) { return true; }

        #endregion
    }
}