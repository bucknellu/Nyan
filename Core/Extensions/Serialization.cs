using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nyan.Core.Extensions
{
    public static class Serialization
    {
        public static string ToXml(this object obj)
        {
            var serializer = new XmlSerializer(obj.GetType());

            using (var writer = new Utf8StringWriter())
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = false }))
            {
                var ns = new XmlSerializerNamespaces();

                ns.Add("", "");

                serializer.Serialize(xmlWriter, obj, ns);
                return writer.ToString();
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

        public static string FromXmlToJson(this string obj)
        {
            var doc = new XmlDocument();
            doc.LoadXml(obj);
            return JsonConvert.SerializeXmlNode(doc);
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

            if (columns == null)
                columns = (from DataColumn col in obj.Table.Columns select col.ColumnName).ToList();

            var colpos = 0;

            sb.Append("{");

            foreach (var column in columns)
            {
                if (obj[colpos].ToString() != "")
                {
                    if (colpos > 0) sb.Append(", ");
                    sb.Append("\"" + column + "\":");

                    if (obj[colpos].GetType().Name == "DateTime")
                    {
                        sb.Append("\"" + ((DateTime)obj[colpos]).ToString("o") + "\"");
                    }
                    else
                    {
                        sb.Append(CleanupJsonData(obj[colpos]));
                    }
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
                if (!isFirstRow)
                    sb.Append(",");


                sb.Append("{");
                for (var i = 0; i < obj.FieldCount; i++)
                {
                    if (obj.GetValue(i) == null) continue;
                    if (obj.GetValue(i) is DBNull) continue;


                    if (i > 0) sb.Append(", ");

                    sb.Append("\"" + obj.GetName(i) + "\":");

                    if (obj.GetValue(i).GetType().Name == "DateTime")
                        sb.Append("\"" + (obj.GetDateTime(i)).ToString("o") + "\"");
                    else
                        sb.Append(CleanupJsonData(obj.GetValue(i).ToString()));
                }
                sb.Append("}");
                isFirstRow = false;
            }

            sb.Append("]");
            return sb.ToString();
        }

        public static string ToJson(this object obj, int pLevels = 0)
        {
            //var s = new JavaScriptSerializer {MaxJsonLength = 50000000};
            //if (pLevels != 0) s.RecursionLimit = pLevels;
            //return s.Serialize(obj);
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch
            {
                return null;
            }
        }

        public static T FromJson<T>(this string obj)
        {
            return obj == null ? default(T) : JsonConvert.DeserializeObject<T>(obj);
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
            try
            {
                return streamReader.ReadToEnd();
            }
            finally
            {
                streamReader.Close();
            }
        }

        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static string CacheKey(this Type baseclass, string id = null, string fullNameAlias = null)
        {
            var basename = fullNameAlias ?? baseclass.FullName;

            return basename + (id == null ? "" : ":" + id);
        }

        public static bool IsBetween<T>(this T item, T start, T end)
        {
            return Comparer<T>.Default.Compare(item, start) >= 0
                   && Comparer<T>.Default.Compare(item, end) <= 0;
        }

        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

            public override string NewLine
            {
                get { return ""; }
            }
        }
    }
}