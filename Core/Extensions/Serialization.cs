using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.String;
using Formatting = Newtonsoft.Json.Formatting;

namespace Nyan.Core.Extensions
{
    public static class Serialization
    {
        private static readonly object OLock = new object();

        public static void ThreadSafeAdd<T>(this List<T> source, T obj)
        {
            lock (OLock) { source.Add(obj); }
        }

        public static string ToXml(this object obj)
        {
            var serializer = new XmlSerializer(obj.GetType());

            using (var writer = new Utf8StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = false }))
                {
                    var ns = new XmlSerializerNamespaces();

                    ns.Add("", "");

                    serializer.Serialize(xmlWriter, obj, ns);
                    return writer.ToString();
                }
            }
        }

        public static byte[] GetBytes(this string str)
        {
            var bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(this byte[] bytes)
        {
            var chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public static string FromXmlToJson(this string obj, bool clean=false)
        {
            var doc = new XmlDocument();
            doc.LoadXml(obj);

            if (!clean) return JsonConvert.SerializeXmlNode(doc );

            var step1 = JsonConvert.SerializeXmlNode(doc, Formatting.None);
            var step2 = step1.FromJson<object>();
            var step3 = step2.ToJson(0, true);

            return step3;
        }

        public static string GetJsonNode(this string obj, string nodeName)
        {
            var jo = JObject.Parse(obj);
            var myTest = jo.Descendants()
                .Where(t => t.Type == JTokenType.Property && ((JProperty)t).Name == nodeName)
                .Select(p => ((JProperty)p).Value)
                .FirstOrDefault();
            return myTest.ToString();
        }

        public static string ToJson(this DataRow obj, List<string> columns = null)
        {
            var sb = new StringBuilder();

            if (columns == null) columns = (from DataColumn col in obj.Table.Columns select col.ColumnName).ToList();

            var colpos = 0;

            sb.Append("{");

            foreach (var column in columns)
            {
                if (obj[colpos].ToString() != "")
                {
                    if (colpos > 0) sb.Append(", ");
                    sb.Append("\"" + column + "\":");

                    if (obj[colpos].GetType().Name == "DateTime") sb.Append("\"" + ((DateTime)obj[colpos]).ToString("o") + "\"");
                    else sb.Append(CleanupJsonData(obj[colpos]));
                }

                colpos++;
            }

            sb.Append("}");

            return sb.ToString();
        }

        public static string ToJson(this DataTable obj)
        {
            var sb = new StringBuilder();
            sb.Append("[");

            var idx = 0;
            var columns = (from DataColumn cols in obj.Columns select cols.ColumnName).ToList();

            foreach (DataRow row in obj.Rows)
            {
                if (idx > 0) sb.Append(",");

                sb.Append(row.ToJson(columns));
                idx++;
            }

            sb.Append("]");

            return sb.ToString();
        }

        public static string ToJson(this DataTableReader obj)
        {
            var sb = new StringBuilder();
            sb.Append("[");

            var isFirstRow = true;

            while (obj.Read())
            {
                if (!isFirstRow) sb.Append(",");

                sb.Append("{");
                for (var i = 0; i < obj.FieldCount; i++)
                {
                    if (obj.GetValue(i) == null) continue;
                    if (obj.GetValue(i) is DBNull) continue;

                    if (i > 0) sb.Append(", ");

                    sb.Append("\"" + obj.GetName(i) + "\":");

                    if (obj.GetValue(i).GetType().Name == "DateTime") sb.Append("\"" + obj.GetDateTime(i).ToString("o") + "\"");
                    else sb.Append(CleanupJsonData(obj.GetValue(i).ToString()));
                }

                sb.Append("}");
                isFirstRow = false;
            }

            sb.Append("]");
            return sb.ToString();
        }

        public static string ToQueryString(this object obj)
        {
            var properties = from p in obj.GetType().GetProperties()
                             where p.GetValue(obj, null) != null
                             select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null).ToString());

            return Join("&", properties.ToArray());
        }

        public static string ToFriendlyUrl(this string title)
        {
            if (title == null) return "";

            const int maxlen = 80;
            var len = title.Length;
            var prevdash = false;
            var sb = new StringBuilder(len);
            char c;

            for (var i = 0; i < len; i++)
            {
                c = title[i];
                if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9')
                {
                    sb.Append(c);
                    prevdash = false;
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    // tricky way to convert to lowercase
                    sb.Append((char)(c | 32));
                    prevdash = false;
                }
                else if (c == ' ' || c == ',' || c == '.' || c == '/' ||
                         c == '\\' || c == '-' || c == '_' || c == '=')
                {
                    if (!prevdash && sb.Length > 0)
                    {
                        sb.Append('-');
                        prevdash = true;
                    }
                }
                else if (c >= 128)
                {
                    var prevlen = sb.Length;
                    sb.Append(RemapInternationalCharToAscii(c));
                    if (prevlen != sb.Length) prevdash = false;
                }

                if (i == maxlen) break;
            }

            if (prevdash) return sb.ToString().Substring(0, sb.Length - 1);
            return sb.ToString();
        }

        public static string RemapInternationalCharToAscii(char c)
        {
            var s = c.ToString().ToLowerInvariant();
            if ("àåáâäãåą".Contains(s)) return "a";
            if ("èéêëę".Contains(s)) return "e";
            if ("ìíîïı".Contains(s)) return "i";
            if ("òóôõöøőð".Contains(s)) return "o";
            if ("ùúûüŭů".Contains(s)) return "u";
            if ("çćčĉ".Contains(s)) return "c";
            if ("żźž".Contains(s)) return "z";
            if ("śşšŝ".Contains(s)) return "s";
            if ("ñń".Contains(s)) return "n";
            if ("ýÿ".Contains(s)) return "y";
            if ("ğĝ".Contains(s)) return "g";
            if (c == 'ř') return "r";
            if (c == 'ł') return "l";
            if (c == 'đ') return "d";
            if (c == 'ß') return "ss";
            if (c == 'Þ') return "th";
            if (c == 'ĥ') return "h";
            if (c == 'ĵ') return "j";
            return "";
        }

        // ReSharper disable once InconsistentNaming

        public static string ToISODateString(this DateTime obj) { return ToISODateString(obj, false); }

        public static string ToISODateString(this DateTime obj, bool includeLocalTimezone)
        {

            if (!includeLocalTimezone) return $"ISODate(\"{obj:o}\")";

            return $"ISODate(\"{obj:o}{TimeZoneInfo.Local.BaseUtcOffset.Hours.ToString().PadLeft(2,'0')}:00\")";
        }

        // ReSharper disable once InconsistentNaming
        public static string ToRawDateHash(this DateTime obj) { return obj.ToString("yyyyMMddHHmmss"); }

        public static DateTime FromRawDateHash(this string obj) { return DateTime.ParseExact(obj, "yyyyMMddHHmmss", new CultureInfo("en-US")); }

        // ReSharper disable once InconsistentNaming
        public static string ToFutureISODateString(this TimeSpan obj) { return DateTime.Now.Add(obj).ToISODateString(); }

        // ReSharper disable once InconsistentNaming
        public static string ToPastISODateString(this TimeSpan obj) { return DateTime.Now.Subtract(obj).ToISODateString(); }

        public static string ToBase64(this string obj) { return obj == null ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(obj)); }

        public static string FromBase64(this string obj)
        {
            if (obj == null) return null;

            var data = Convert.FromBase64String(obj);
            var decodedString = Encoding.UTF8.GetString(data);

            return decodedString;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this NameValueCollection col)
        {
            var dict = new Dictionary<TKey, TValue>();
            var keyConverter = TypeDescriptor.GetConverter(typeof(TKey));
            var valueConverter = TypeDescriptor.GetConverter(typeof(TValue));

            foreach (string name in col)
            {
                TKey key = (TKey)keyConverter.ConvertFromString(name);
                TValue value = (TValue)valueConverter.ConvertFromString(col[name]);
                dict.Add(key, value);
            }

            return dict;
        }

        public static string ToJson(this object obj, int pLevels = 0) { return ToJson(obj, 0, false); }

        public static string ToJson(this object obj, int pLevels, bool ignoreEmpty)
        {
            //return s.Serialize(obj);
            try
            {
                var result = JsonConvert.SerializeObject(obj);

                if (!ignoreEmpty) return result;

                var temp = JObject.Parse(result);
                temp.Descendants()
                    .OfType<JProperty>()
                    .Where(attr => attr.Value.ToString() == "" || attr.Value == null)
                    .ToList() // you should call ToList because you're about to changing the result, which is not possible if it is IEnumerable
                    .ForEach(attr => attr.Remove()); // removing unwanted attributes

                result = temp.ToString();

                return result;
            } catch { return null; }


       
        }

        public static object ToJObject(this object src) { return JObject.Parse(src.ToJson()); }

        public static string CleanSqlFormatting(this string source)
        {
            var ret = source.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
            while (ret.IndexOf("  ", StringComparison.Ordinal) != -1) ret = ret.Replace("  ", " ");
            return ret;
        }

        public static IDictionary<string, object> ToKeyValueDictionary(this string source) { return source.FromJson<IDictionary<string, object>>(); }

        public static Dictionary<string, string> ToPathValueDictionary(this JObject source)
        {
            var ret = new Dictionary<string, string>();

            foreach (var jToken in (JToken)source)
            {
                var t = (JProperty)jToken;

                var k = t.Name;
                var v = t.Value;

                if (v is JObject) ret = ret.Concat(ToPathValueDictionary((JObject)v)).ToDictionary(x => x.Key, x => x.Value);
                else ret.Add(t.Path, v.ToString());
            }

            return ret;
        }

        public static T FromJson<T>(this string obj) { return obj == null ? default(T) : JsonConvert.DeserializeObject<T>(obj); }

        public static object FromJson(this string obj, Type destinyFormat, bool asList)
        {
            var type = destinyFormat;

            if (asList)
            {
                var genericListType = typeof(List<>);

                var specificListType = genericListType.MakeGenericType(destinyFormat);
                type = ((IEnumerable<object>)Activator.CreateInstance(specificListType)).GetType();

            }

            if (obj == null) return null;
            return JsonConvert.DeserializeObject(obj, type);
        }

        public static byte[] ToSerializedBytes(this object obj)
        {
            byte[] result;
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, obj);
                stream.Flush();
                result = stream.ToArray();
            }

            return result;
        }

        public static T FromSerializedBytes<T>(this byte[] obj)
        {
            using (var stream = new MemoryStream(obj))
            {
                var ser = new BinaryFormatter();
                return (T)ser.Deserialize(stream);
            }
        }

        private static string CleanupJsonData(object data)
        {
            var ret = Reflections.IsNumericType(data)
                ? data.ToString()
                : "\"" +
                  data.ToString()
                      .Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace(Environment.NewLine, "\\n") +
                  "\"";

            return ret;
        }

        public static string GetString(this HttpWebResponse a)
        {
            var streamReader = new StreamReader(a.GetResponseStream(), true);
            try { return streamReader.ReadToEnd(); }
            finally { streamReader.Close(); }
        }

        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) return true;
                toCheck = toCheck.BaseType;
            }

            return false;
        }

        public static string CacheKey(this Type baseclass, string id = null, string fullNameAlias = null)
        {
            return CacheKey(baseclass, id, fullNameAlias, null);
        }

        public static string CacheKey(this Type baseclass, string id, string fullNameAlias, string suffix = null)
        {
            var basename = (fullNameAlias ?? baseclass.FullName) + suffix;

            return basename + (id == null ? "" : ":" + id);
        }

        public static bool IsBetween<T>(this T item, T start, T end)
        {
            return Comparer<T>.Default.Compare(item, start) >= 0
                   && Comparer<T>.Default.Compare(item, end) <= 0;
        }

        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;

            public override string NewLine => "";
        }
    }
}