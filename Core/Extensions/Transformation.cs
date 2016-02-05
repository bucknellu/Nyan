using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using Nyan.Core.Factories;
using System.Security.Cryptography;
using System.Text;

namespace Nyan.Core.Extensions
{
    public static class Transformation
    {
        public enum ESafeArrayMode
        {
            Remove,
            Allow
        }
        public static string SafeArray(this string source, string criteria = "", string keySeparator = "=", string elementSeparator = ",", ESafeArrayMode mode = ESafeArrayMode.Remove)
        {
            if (source == null) return "";
            var lineCol = source.Split(elementSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            var criteriaCol = criteria.ToLower().Split(',').ToList();
            var ret = new List<string>();
            var compiledRet = "";

            foreach (var i in lineCol)
            {
                var item = i.Split(keySeparator.ToCharArray());

                var key = item[0].Trim().ToLower();


                bool allow = false;

                switch (mode)
                {
                    case ESafeArrayMode.Remove:
                        allow = !(criteriaCol.Contains(key));
                        break;
                    case ESafeArrayMode.Allow:
                        allow = (criteriaCol.Contains(key));
                        break;
                }

                if (allow) ret.Add(i);
            }

            foreach (var item in ret)
            {
                if (compiledRet != "") compiledRet += elementSeparator;
                compiledRet += item;
            }

            return compiledRet;
        }

        public static string Md5Hash(this string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes and create a string.
                StringBuilder sBuilder = new StringBuilder();

                //format each string as hexidecimal 
                foreach (byte b in data)
                {
                    sBuilder.Append(b.ToString("x2"));
                }

                return sBuilder.ToString();
            }
        }


        public static bool MD5HashCheck(this string input, string hash)
        {
            // Hash the input. 
            string hashOfInput = Md5Hash(input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return 0 == comparer.Compare(hashOfInput, hash);
        }

        public static List<string> BlackListedModules = new List<string>
        {
            "System.Linq.Enumerable",
            "System.Collections.Generic.List",
            "System.Data.Common.DbCommand",
            "Oracle.DataAccess.Client.OracleCommand",
            "Dapper.SqlMapper+<QueryImpl>"
        };


        public static string Encrypt(this string value)
        {
            return Settings.Current.Encryption.Encrypt(value);
        }

        public static string Decrypt(this string value)
        {
            return Settings.Current.Encryption.Decrypt(value);
        }

        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + " ..";
        }

        public static bool IsNumeric(this object refObj)
        {
            if (refObj == null) return false;

            long n;
            var isNumeric = long.TryParse(refObj.ToString(), out n);
            return isNumeric;

        }

        public static IEnumerable<string> SplitInChunksUpTo(this string str, int maxChunkSize)
        {
            for (var i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        public static ShortGuid ToShortGuid(this Guid oRef)
        {
            return new ShortGuid(oRef);
        }

        public static string FancyString(this StackTrace source)
        {
            var ret = "";

            var stFrames = source.GetFrames();

            var validFrames = stFrames.ToList();

            validFrames.Reverse();

            var mon = new Dictionary<string, string>();
            mon["mod"] = "";
            mon["type"] = "";
            var probe = "";

            foreach (var vf in validFrames)
            {
                if (vf.GetMethod().ReflectedType == null) continue;

                probe = vf.GetMethod().ReflectedType.FullName;
                if (BlackListedModules.Any(s => probe.IndexOf(s, StringComparison.OrdinalIgnoreCase) != -1))
                    continue;

                if (ret != "") ret += " > ";

                if (mon["mod"] == "")
                {
                    mon["mod"] = vf.GetMethod().Module.ToString();
                    ret += mon["mod"] + " - ";
                }

                if (mon["type"] != probe)
                {
                    mon["type"] = probe;
                    ret += vf.GetMethod().ReflectedType.FullName + ":";
                }

                ret += vf.GetMethod().Name;

                if (vf.GetFileColumnNumber() != 0)

                    ret += "[{0},{1}]".format(vf.GetFileColumnNumber(), vf.GetFileLineNumber());
            }

            return ret;
        }

        public static string TrimSql(this string s)
        {
            return s
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Replace("  ", " ")
                .Replace("  ", " ")
                .Replace("  ", " ")
                .Replace("  ", " ")
                .Replace("  ", " ")
                .Replace("  ", " ")
                .Replace("  ", " ")
                ;
        }

        public static IDictionary<string, object> AddProperty(this object obj, string name, object value)
        {
            var dictionary = obj.ToDictionary();
            dictionary.Add(name, value);
            return dictionary;
        }

        public static T? ToNullable<T>(this string s) where T : struct
        {
            var result = new T?();
            try
            {
                if (!String.IsNullOrEmpty(s) && s.Trim().Length > 0)
                {
                    var conv = TypeDescriptor.GetConverter(typeof(T));
                    result = (T)conv.ConvertFrom(s);
                }
            }
            catch { }
            return result;
        }

        public static T? ToNullable<T>(this long s) where T : struct
        {
            var result = new T?();
            try
            {
                var conv = TypeDescriptor.GetConverter(typeof(T));
                result = (T)conv.ConvertFrom(s);
            }
            catch { }
            return result;
        }

        public static IDictionary<string, object> ToDictionary(this object obj)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            var properties = TypeDescriptor.GetProperties(obj);
            foreach (PropertyDescriptor property in properties)
            {
                result.Add(property.Name, property.GetValue(obj));
            }
            return result;
        }

        public static string ToCommaSeparatedString(this List<string> obj)
        {
            return obj.Aggregate((i, j) => i + ", " + j);
        }

        public static string format(this string source, params object[] parms)
        {
            return String.Format(source, parms);
        }

        public static List<List<T>> Split<T>(this List<T> items, int sliceSize = 30)
        {
            var list = new List<List<T>>();
            for (var i = 0; i < items.Count; i += sliceSize)
                list.Add(items.GetRange(i, Math.Min(sliceSize, items.Count - i)));
            return list;
        }

        public static bool TryCast<T>(ref T t, object o)
        {
            if (!(o is T))
            {
                return false;
            }

            t = (T)o;
            return true;
        }

        public static T ConvertTo<T>(ref object input)
        {
            return (T)Convert.ChangeType(input, typeof(T));
        }

        public static object ToConcrete<T>(this ExpandoObject dynObject)
        {
            object instance = Activator.CreateInstance<T>();
            var dict = dynObject as IDictionary<string, object>;
            var targetProperties = instance.GetType().GetProperties();

            foreach (var property in targetProperties)
            {
                object propVal;
                if (dict.TryGetValue(property.Name, out propVal))
                {
                    property.SetValue(instance, propVal, null);
                }
            }

            return instance;
        }

        public static ExpandoObject ToExpando(this object staticObject)
        {
            var expando = new ExpandoObject();
            var dict = expando as IDictionary<string, object>;
            var properties = staticObject.GetType().GetProperties();

            foreach (var property in properties)
            {
                dict[property.Name] = property.GetValue(staticObject, null);
            }

            return expando;
        }
    }
}