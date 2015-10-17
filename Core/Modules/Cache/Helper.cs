using Nyan.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Cache
{
    public static class Helper
    {
        #region Cache Management methods

        public static List<T> FetchCacheableListResultByKey<T>(Func<string, List<T>> method, string key)
        {
            var cacheid = typeof(T).CacheKey(key);

            var cache = Settings.Current.Cache[cacheid].FromJson<List<T>>();

            if (cache != null)
            {
                return cache;
            }

            var ret = method(key);

            //Settings.Current.Log.Add("CACHE STO " + cacheid);
            Settings.Current.Cache[cacheid] = ret.ToJson();

            return ret;
        }
        public static T FetchCacheableSingleResultByKey<T>(Func<string, T> method, string key, string baseType = null)
        {
            var cacheid = typeof(T).CacheKey(key, baseType);

            var cache = Settings.Current.Cache[cacheid].FromJson<T>();

            if (cache != null)
            {
                return cache;
            }

            var ret = method(key);

            //Settings.Current.Log.Add("CACHE STO " + cacheid);
            Settings.Current.Cache[cacheid] = ret.ToJson();

            return ret;
        }
        public static T FetchCacheableResultSingleton<T>(Func<T> method, object singletonLock, string namespaceSpec = null, int timeOutSeconds = 600)
        {
            string cacheid;

            if (namespaceSpec == null)
            {
                cacheid = typeof(T).CacheKey("s");

                try
                {
                    if (typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                        if (typeof(T).GetGenericArguments()[0].IsPrimitiveType())
                            throw new ArgumentOutOfRangeException(
                                "Invalid cache source - list contains primitive type. Specify namespaceSpec.");
                        else
                            cacheid = typeof(T).GetGenericArguments()[0].CacheKey("s");
                }
                catch
                {
                }
            }
            else
            {
                cacheid = namespaceSpec + ":s";
            }

            var cache = Settings.Current.Cache[cacheid].FromJson<T>();
            if (cache != null)
            {
                //Settings.Current.Log.Add("CACHE HIT " + cacheid);
                return cache;
            }

            //Settings.Current.Log.Add("CACHE NIL " + cacheid);

            lock (singletonLock)
            {
                cache = Settings.Current.Cache[cacheid].FromJson<T>();
                if (cache != null)
                {
                    //Settings.Current.Log.Add("CACHE LAG " + cacheid);
                    return cache;
                }

                var ret = method();

                //Settings.Current.Log.Add("CACHE STO " + cacheid);
                Settings.Current.Cache[cacheid, null, timeOutSeconds] = ret.ToJson();
                cache = ret;
            }

            return cache;
        }

        #endregion
    }
}
