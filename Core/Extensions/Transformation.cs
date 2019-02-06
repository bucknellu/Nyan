using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using Nyan.Core.Factories;
using Nyan.Core.Settings;

namespace Nyan.Core.Extensions
{
    public static class Transformation
    {
        public enum ESafeArrayMode
        {
            Remove,

            Allow
        }

        private static readonly Random Rnd = new Random();

        public static List<string> BlackListedModules = new List<string>
        {
            "System.Linq.Enumerable",
            "System.Collections.Generic.List",
            "System.Data.Common.DbCommand",
            "Oracle.DataAccess.Client.OracleCommand",
            "Dapper.SqlMapper+<QueryImpl>",
            "System.Web.Http.Controllers",
            "System.Runtime.CompilerServices",
            "System.Runtime.ExceptionServices",
            "CommonLanguageRuntimeLibrary"
        };

        public static DateTime RoundUp(this DateTime dt, TimeSpan d)
        {
            //https://stackoverflow.com/a/7029464/1845714
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        public static string ToOrdinal(this int num)
        {
            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13: return num + "th";
            }

            switch (num % 10)
            {
                case 1: return num + "st";
                case 2: return num + "nd";
                case 3: return num + "rd";
                default: return num + "th";
            }
        }

        public static Image FromPathToImage(this string source) { return new Bitmap(source); }

        public static DateTime Next(this DateTime date, DayOfWeek dayOfWeek)
        {
            // https://stackoverflow.com/a/3284486/1845714
            return date.AddDays((dayOfWeek < date.DayOfWeek ? 7 : 0) + dayOfWeek - date.DayOfWeek);
        }

        public static void CopyValues<T>(this T source, T target, bool copyWhenSourceIsNull = false, bool copyWhenTargetIsNotNull = true)
        {
            var t = typeof(T);

            var properties = t.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source, null);
                var currValue = prop.GetValue(target, null);

                if (value == null && !copyWhenSourceIsNull) continue;
                if (currValue == null || copyWhenTargetIsNotNull) prop.SetValue(target, value, null);
            }
        }

        // https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        public static string ToHex(this byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba) hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static List<string> GetTemplateKeys(this string body)
        {
            if (body == null) return null;

            const string pattern = @"{{(.*?)}}";
            var matches = Regex.Matches(body, pattern).Cast<Match>().Select(m => m.Value).ToList();
            return matches;
        }

        public static string TemplateFill(this string body, object sourceObj)
        {
            var tmp = body;

            var source = JToken.Parse(sourceObj.ToJson());

            var keys = body.GetTemplateKeys();

            foreach (var key in keys)
            {
                var tokenName = key.Substring(2, key.Length - 4);
                var probe = source.SelectToken(tokenName);
                if (probe != null) tmp = tmp.Replace(key, probe.ToString());
            }

            return tmp;
        }

        public static string StripHtml(this string input) { return input == null ? null : Regex.Replace(input, "<.*?>", string.Empty); }

        public static IEnumerable<T> ToInstances<T>(this IEnumerable<Type> source) { return source.Select(i => (T) Activator.CreateInstance(i, new object[] { })).ToList(); }
        public static T ToInstance<T>(this Type source) { return (T) Activator.CreateInstance(source, new object[] { }); }

        public static IEnumerable<List<T>> SplitList<T>(List<T> items, int nSize = 30)
        {
            // https://stackoverflow.com/questions/11463734/split-a-list-into-smaller-lists-of-n-size

            for (var i = 0; i < items.Count; i += nSize) yield return items.GetRange(i, Math.Min(nSize, items.Count - i));
        }

        public static bool MatchWildcardPattern(this string value, string pattern)
        {
            var isMatch = Regex.IsMatch(value, pattern.WildCardToRegular());
            return isMatch;
        }

        public static T Random<T>(this IEnumerable<T> source)
        {
            if (source == null) return default(T);

            var enumerable = source.ToList();

            var r = Rnd.Next(enumerable.Count());
            return enumerable[r];
        }

        private static string WildCardToRegular(this string value) { return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$"; }

        public static string ToQueryString(this Dictionary<string, string> obj)
        {
            var properties = from p in obj
                where p.Value != null
                select p.Key + "=" + HttpUtility.UrlEncode(p.Value);

            return string.Join("&", properties.ToArray());
        }

        public static string ToQueryString(this object obj)
        {
            var properties = from p in obj.GetType().GetProperties()
                where p.GetValue(obj, null) != null
                select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null).ToString());

            return string.Join("&", properties.ToArray());
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

                var allow = false;

                switch (mode)
                {
                    case ESafeArrayMode.Remove:
                        allow = !criteriaCol.Contains(key);
                        break;
                    case ESafeArrayMode.Allow:
                        allow = criteriaCol.Contains(key);
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

        public static string Md5Hash(this string input, string salt = null)
        {
            if (input == null) return null;

            using (var md5Hash = MD5.Create())
            {
                var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input + salt));

                // Create a new Stringbuilder to collect the bytes and create a string.
                var sBuilder = new StringBuilder();

                //format each byte as hexadecimal 
                foreach (var b in data) sBuilder.Append(b.ToString("x2"));

                return sBuilder.ToString();
            }
        }

        // https://stackoverflow.com/a/5665784/1845714
        public static string Sha512Hash(this string input, string salt = null)
        {
            if (input == null) return null;

            using (var hash = SHA512.Create())
            {
                var data = hash.ComputeHash(Encoding.UTF8.GetBytes(input + salt));

                // Create a new Stringbuilder to collect the bytes and create a string.
                var sBuilder = new StringBuilder();

                //format each byte as hexadecimal 

                foreach (var b in data) sBuilder.Append(b.ToString("x2"));

                return sBuilder.ToString();
            }
        }

        public static string MetaHash(this string input, string salt = null)
        {
            var p1 = input.Md5Hash(salt);
            var p2 = input.Sha512Hash(salt);

            var p3 = p1 != null && p2 != null ? "-" : null;

            return p1 + p3 + p2;
        }

        // https://weblogs.asp.net/haithamkhedre/generate-guid-from-any-string-using-c
        public static Guid StringToGuid(this string value)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            var md5Hasher = MD5.Create();
            // Convert the input string to a byte array and compute the hash.
            var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(value));
            return new Guid(data);
        }

        public static bool MD5HashCheck(this string input, string hash)
        {
            // Hash the input. 
            var hashOfInput = Md5Hash(input);

            // Create a StringComparer an compare the hashes.
            var comparer = StringComparer.OrdinalIgnoreCase;

            return 0 == comparer.Compare(hashOfInput, hash);
        }

        public static string Encrypt(this string value) { return Current.Encryption.Encrypt(value); }
        public static string Decrypt(this string value) { return Current.Encryption.Decrypt(value); }
        public static string Truncate(this string value, int maxChars) { return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "..."; }

        public static bool IsNumeric(this object refObj)
        {
            if (refObj == null) return false;

            long n;
            var isNumeric = long.TryParse(refObj.ToString(), out n);
            return isNumeric;
        }

        public static IEnumerable<string> SplitInChunksUpTo(this string str, int maxChunkSize)
        {
            for (var i = 0; i < str.Length; i += maxChunkSize) yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        public static ShortGuid ToShortGuid(this Guid oRef) { return new ShortGuid(oRef); }
        public static string FancyString(this Exception source) { return new StackTrace(source, true).FancyString(); }

        public static string ToSummary(this Exception ex)
        {
            var output = "";

            output += ex.Message + new StackTrace(ex, true).FancyString();
            if (ex.InnerException != null) output += "; " + ex.InnerException.ToSummary();

            return output;
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
                if (BlackListedModules.Any(s => probe.IndexOf(s, StringComparison.OrdinalIgnoreCase) != -1)) continue;

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

                if (vf.GetFileColumnNumber() != 0) ret += "[L{1} C{0}]".format(vf.GetFileColumnNumber(), vf.GetFileLineNumber());
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

        public static ICollection<T> ToCollection<T>(this List<T> items) where T : class
        {
            var ret = new Collection<T>();
            foreach (var t in items) ret.Add(t);
            return ret;
        }

        public static T? ToNullable<T>(this string s) where T : struct
        {
            var result = new T?();
            try
            {
                if (!string.IsNullOrEmpty(s) && s.Trim().Length > 0)
                {
                    var conv = TypeDescriptor.GetConverter(typeof(T));
                    result = (T) conv.ConvertFrom(s);
                }
            } catch { }

            return result;
        }

        public static T? ToNullable<T>(this long s) where T : struct
        {
            var result = new T?();
            try
            {
                var conv = TypeDescriptor.GetConverter(typeof(T));
                result = (T) conv.ConvertFrom(s);
            } catch { }

            return result;
        }

        public static IDictionary<string, object> ToDictionary(this object obj)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            var properties = TypeDescriptor.GetProperties(obj);
            foreach (PropertyDescriptor property in properties) result.Add(property.Name, property.GetValue(obj));
            return result;
        }

        public static T ToObject<T>(this IDictionary<string, object> source)
            where T : class, new()
        {
            var someObject = new T();
            var someObjectType = someObject.GetType();

            foreach (var item in source) someObjectType.GetProperty(item.Key).SetValue(someObject, item.Value, null);

            return someObject;
        }

        public static string ToCommaSeparatedString(this List<string> obj) { return obj.Aggregate((i, j) => i + ", " + j); }
        public static string format(this string source, params object[] parms) { return string.Format(source, parms); }

        public static List<List<T>> Split<T>(this List<T> items, int sliceSize = 30)
        {
            var list = new List<List<T>>();
            for (var i = 0; i < items.Count; i += sliceSize) list.Add(items.GetRange(i, Math.Min(sliceSize, items.Count - i)));
            return list;
        }

        public static bool TryCast<T>(ref T t, object o)
        {
            if (!(o is T)) return false;

            t = (T) o;
            return true;
        }

        public static T ConvertTo<T>(ref object input) { return (T) Convert.ChangeType(input, typeof(T)); }

        public static object ToConcrete<T>(this ExpandoObject dynObject)
        {
            object instance = Activator.CreateInstance<T>();
            var dict = dynObject as IDictionary<string, object>;
            var targetProperties = instance.GetType().GetProperties();

            foreach (var property in targetProperties)
            {
                object propVal;
                if (dict.TryGetValue(property.Name, out propVal)) property.SetValue(instance, propVal, null);
            }

            return instance;
        }

        public static ExpandoObject ToExpando(this object staticObject)
        {
            var expando = new ExpandoObject();
            var dict = expando as IDictionary<string, object>;
            var properties = staticObject.GetType().GetProperties();

            foreach (var property in properties) dict[property.Name] = property.GetValue(staticObject, null);

            return expando;
        }

        public static bool StringsAreSimilar(string baseStr, string compareTo)
        {
            if (baseStr == compareTo) return true;

            var s1Words = baseStr.Split(' ');
            var s2Words = compareTo.Split(' ');

            if (s1Words.Length != s2Words.Length) return false;

            //This is needed to protect against typos and inconsistencies in data such as grill vs grille
            for (var i = 0; i < s1Words.Length; i++)
                try
                {
                    if (s1Words[i].SoundEx() != s2Words[i].SoundEx()) return false;
                } catch { return false; }

            return true;
        }

        private static string SoundEx(this string str)
        {
            var result = new StringBuilder();
            if (!string.IsNullOrEmpty(str))
            {
                string previousCode = "", currentCode = "", currentLetter = "";
                result.Append(str.Substring(0, 1));

                for (var i = 1; i < str.Length; i++)
                {
                    currentLetter = str.Substring(i, 1).ToLower();
                    currentCode = "";

                    if ("bfpv".IndexOf(currentLetter, StringComparison.Ordinal) > -1) currentCode = "1";
                    else if ("cgjkqsxz".IndexOf(currentLetter, StringComparison.Ordinal) > -1) currentCode = "2";
                    else if ("dt".IndexOf(currentLetter, StringComparison.Ordinal) > -1) currentCode = "3";
                    else if (currentLetter == "l") currentCode = "4";
                    else if ("mn".IndexOf(currentLetter, StringComparison.Ordinal) > -1) currentCode = "5";
                    else if (currentLetter == "r") currentCode = "6";

                    if (currentCode != previousCode) result.Append(currentCode);
                }
            }

            if (result.Length < 4) result.Append(new string('0', 4 - result.Length));

            return result.ToString().ToUpper();
        }

        public static bool IgnoreCaseContains(this IEnumerable<string> list, string lookupStr)
        {
            lookupStr = lookupStr.ToLower();
            foreach (var str in list)
            {
                var s = str.ToLower();
                if (s.Equals(lookupStr)) return true;
            }

            return false;
        }

        public static string ProperCase(this string str)
        {
            var words = str.Split(' ');
            for (var i = 0; i < words.Length; i++) words[i] = words[i].Substring(0, 1).ToUpper() + words[i].ToLower().Substring(1, words[i].Length - 1);

            return string.Join(" ", words);
        }

        public static string ToString(this JValue val) { return val.ToObject<string>(); }

        public static bool IsValidEmail(this string strIn)
        {
            if (string.IsNullOrEmpty(strIn)) return false;

            // Use IdnMapping class to convert Unicode domain names.
            try { strIn = Regex.Replace(strIn, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200)); } catch { return false; }

            // Return true if strIn is in valid e-mail format.
            try
            {
                return Regex.IsMatch(strIn,
                                     @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                                     @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                                     RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            } catch (RegexMatchTimeoutException) { return false; }
        }

        private static string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            var idn = new IdnMapping();

            var domainName = match.Groups[2].Value;
            domainName = idn.GetAscii(domainName);

            return match.Groups[1].Value + domainName;
        }

        public static string FromNumberStringToString(this string source, int numDec = 2)
        {
            string ret;

            if (source == null) return null;

            try
            {
                var num = Convert.ToDecimal(source);

                var patt = "0:#";

                if (numDec > 0) patt += "." + new string('#', numDec);

                ret = string.Format("{" + patt + "}", num);
            } catch (Exception e) { ret = source; }

            return ret;
        }
    }
}