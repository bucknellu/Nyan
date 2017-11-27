using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Maintenance;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Data
{
    public class ComplexMicroEntity<T, TU>
        where T : ComplexMicroEntity<T, TU>, new()
        where TU : MicroEntity<TU>
    {

        public static List<T> Get()
        {

            // 2-step in order for guarantee caching.

            var src = MicroEntity<TU>.Get().ToList();
            var ret = new List<T>();

            foreach (var u in src)
            {
                ret.Add(Get(u.GetEntityIdentifier()));
            }

            return ret;
        }

        public static T Get(long identifier, string referenceField = null)
        {
            return Get(identifier.ToString(CultureInfo.InvariantCulture), referenceField);
        }

        public static T Get(string identifier, string referenceField = null)
        {

            if (identifier == null) return null;
            if (identifier.ToLower().Trim() == "new") return new T();

            var cacheid = typeof(T).CacheKey(identifier);

            if (Current.Cache.OperationalStatus == EOperationalStatus.Operational)
            {

                var cache = Current.Cache[cacheid].FromJson<T>();
                if (cache != null)
                { return cache; }
            }


            var ret = new T();
            TU probe;

            if (referenceField == null)
                probe = MicroEntity<TU>.Get(identifier);
            else
            {
                var lprobe = MicroEntity<TU>.ReferenceQueryByField(referenceField, identifier).ToList();
                if (lprobe.Count > 0)
                    probe = lprobe[0];
                else
                    return null;
            }

            var ret2 = new T();

            try { ret2 = ret.CastToComplexType(probe); }
            catch (Exception ee)
            {
                Current.Log.Add(ee);
            }

            Current.Cache[cacheid] = ret2.ToJson();

            return ret2;
        }

        public static T Get(TU probe)
        {
            return new T().CastToComplexType(probe);
        }

        public static List<T> ConvertFromBaseList(List<TU> source)
        {
            var c = new Clicker($"{typeof(TU).Name}.ConvertFromBaseList", source.Count);

            var ret = source.Select(x =>
            {
                c.Click();
                return new T().CastToComplexType(x);
            }).ToList();

            c.End();

            return ret;
        }

        public virtual T CastToComplexType(TU probe)
        {
            //Must ALWAYS be overridden by implementing Class.
            throw new NotImplementedException();
        }

        public static void FlushCacheEntry(string identifier)
        {
            if (Current.Cache.OperationalStatus != EOperationalStatus.Operational) return;

            var cacheid = typeof(T).CacheKey(identifier);

            Current.Log.Add("CACHE KILL " + cacheid);

            Current.Cache.Remove(cacheid);
        }
        public static void FlushCacheEntry(long identifier)
        {
            FlushCacheEntry(identifier.ToString());
        }

        public static void SetCacheEntry(long identifier, object content)
        {
            SetCacheEntry(identifier.ToString(), content);
        }
        public static void SetCacheEntry(string key, object content)
        {
            var cacheid = typeof(T).CacheKey(key);
            Current.Cache[cacheid] = content.ToJson();
        }
    }
}