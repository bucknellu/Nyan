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
    public static class Extensions
    {
        private static readonly Type[] PrimitiveTypes =
        {
            typeof (string),
            typeof (decimal),
            typeof (DateTime),
            typeof (DateTimeOffset),
            typeof (TimeSpan),
            typeof (Guid),
            typeof (Enum),
            typeof (byte[]),
            typeof (object)
        };

        public static List<string> BlackListedModules = new List<string>
        {
            "System.Linq.Enumerable",
            "System.Collections.Generic.List",
            "System.Data.Common.DbCommand",
            "Oracle.DataAccess.Client.OracleCommand",
            "Dapper.SqlMapper+<QueryImpl>"
        };

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

        public static T CreateInstance<T>(this Type typeRef)
        {
            return (T)Activator.CreateInstance(typeRef);
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

        public static bool IsPrimitiveType(this Type type)
        {
            return
                type.IsValueType ||
                type.IsPrimitive ||
                PrimitiveTypes.Contains(type) ||
                Convert.GetTypeCode(type) != TypeCode.Object;
        }

        public static T GetObject<T>(this IDictionary<string, object> dict, Dictionary<string, string> translationDictionary = null)
        {
            var type = typeof(T);

            var obj = Activator.CreateInstance(type);

            foreach (var kv in dict)
            {
                var propertyNameRes = kv.Key;

                if (translationDictionary != null)
                    if (translationDictionary.ContainsValue(propertyNameRes))
                        propertyNameRes = translationDictionary.FirstOrDefault(x => x.Value == propertyNameRes).Key;

                var k = type.GetProperty(propertyNameRes,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                var val = kv.Value;

                if (k == null) continue;

                var kt = k.PropertyType;


                if (k.PropertyType.IsPrimitiveType())
                {
                    try
                    {
                        if (val is decimal) val = Convert.ToInt64(val);
                        if (val is short && kt == typeof(bool)) val = (Convert.ToInt16(val) == 1);
                        if (val is long && kt == typeof(string)) val = val.ToString();
                        if (kt == typeof(decimal)) val = Convert.ToDecimal(val);
                        if (kt == typeof(short)) val = Convert.ToInt16(val);
                        if (kt == typeof(int)) val = Convert.ToInt32(val);
                        if (kt == typeof(long)) val = Convert.ToInt64(val);
                        if (kt == typeof(Guid)) if (val != null) val = new Guid(val.ToString());
                        if (kt.IsEnum) val = Enum.Parse(k.PropertyType, val.ToString());

                        k.SetValue(obj, val);

                    }
                    catch (Exception e)
                    {
                        Settings.Current.Log.Add(e);
                        throw;
                    }

                }
                else
                {
                    k.SetValue(obj, JsonConvert.DeserializeObject(kv.Value.ToString(), kt));
                }
            }

            return (T)obj;
        }

        public static MethodInfo GetMethodExt(this Type thisType, string name, params Type[] parameterTypes)
        {
            return GetMethodExt(thisType,
                name,
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.FlattenHierarchy,
                parameterTypes);
        }

        public static MethodInfo GetMethodExt(this Type thisType, string name, BindingFlags bindingFlags, params Type[] parameterTypes)
        {
            MethodInfo matchingMethod = null;

            // Check all methods with the specified name, including in base classes
            GetMethodExt(ref matchingMethod, thisType, name, bindingFlags, parameterTypes);

            // If we're searching an interface, we have to manually search base interfaces
            if (matchingMethod == null && thisType.IsInterface)
            {
                foreach (var interfaceType in thisType.GetInterfaces())
                    GetMethodExt(ref matchingMethod,
                        interfaceType,
                        name,
                        bindingFlags,
                        parameterTypes);
            }

            return matchingMethod;
        }

        private static void GetMethodExt(ref MethodInfo matchingMethod, Type type, string name, BindingFlags bindingFlags, params Type[] parameterTypes)
        {
            // Check all methods with the specified name, including in base classes
            foreach (MethodInfo methodInfo in type.GetMember(name,
                MemberTypes.Method,
                bindingFlags))
            {
                // Check that the parameter counts and types match, 
                // with 'loose' matching on generic parameters
                var parameterInfos = methodInfo.GetParameters();
                if (parameterInfos.Length == parameterTypes.Length)
                {
                    var i = 0;
                    for (; i < parameterInfos.Length; ++i)
                    {
                        if (!parameterInfos[i].ParameterType
                            .IsSimilarType(parameterTypes[i]))
                            break;
                    }
                    if (i == parameterInfos.Length)
                    {
                        if (matchingMethod == null)
                            matchingMethod = methodInfo;
                        else
                            throw new AmbiguousMatchException(
                                "More than one matching method found!");
                    }
                }
            }
        }

        private static bool IsSimilarType(this Type thisType, Type type)
        {
            // Ignore any 'ref' types
            if (thisType.IsByRef)
                thisType = thisType.GetElementType();
            if (type.IsByRef)
                type = type.GetElementType();

            // Handle array types
            if (thisType.IsArray && type.IsArray)
                return thisType.GetElementType().IsSimilarType(type.GetElementType());

            // If the types are identical, or they're both generic parameters 
            // or the special 'T' type, treat as a match
            if (thisType == type || ((thisType.IsGenericParameter || thisType == typeof(GetMethodExtT))
                                     && (type.IsGenericParameter || type == typeof(GetMethodExtT))))
                return true;

            // Handle any generic arguments
            if (thisType.IsGenericType && type.IsGenericType)
            {
                var thisArguments = thisType.GetGenericArguments();
                var arguments = type.GetGenericArguments();
                if (thisArguments.Length == arguments.Length)
                {
                    return !thisArguments.Where((t, i) => !t.IsSimilarType(arguments[i])).Any();
                }
            }

            return false;
        }

        private static string CleanupJsonData(object data)
        {
            var ret = IsNumericType(data)
                ? data.ToString()
                : "\"" +
                  data.ToString()
                      .Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace(Environment.NewLine, "\\n") +
                  "\"";

            return ret;
        }

        private static bool IsNumericType(object obj)
        {
            var numericTypes = new HashSet<Type>
            {
                typeof (byte),
                typeof (sbyte),
                typeof (ushort),
                typeof (uint),
                typeof (ulong),
                typeof (short),
                typeof (int),
                typeof (long),
                typeof (decimal),
                typeof (double),
                typeof (float)
            };
            return numericTypes.Contains(obj.GetType());
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

        public static List<List<T>> Split<T>(this List<T> items, int sliceSize = 30)
        {
            var list = new List<List<T>>();
            for (var i = 0; i < items.Count; i += sliceSize)
                list.Add(items.GetRange(i, Math.Min(sliceSize, items.Count - i)));
            return list;
        }

        public static IEnumerable<string> SplitInChunksUpTo(this string str, int maxChunkSize)
        {
            for (var i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
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

        public static T? ToNullable<T>(this string s) where T : struct
        {
            var result = new T?();
            try
            {
                if (!string.IsNullOrEmpty(s) && s.Trim().Length > 0)
                {
                    var conv = TypeDescriptor.GetConverter(typeof(T));
                    result = (T)conv.ConvertFrom(s);
                }
            }
            catch
            {
            }
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
            catch
            {
            }
            return result;
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

        public static void CopyPropertiesTo<T, TU>(this T source, TU dest)
        {
            var sourceProps = typeof(T).GetProperties().Where(x => x.CanRead).ToList();
            var destProps = typeof(TU).GetProperties()
                .Where(x => x.CanWrite)
                .ToList();


            if (source == null) return;

            foreach (var sourceProp in sourceProps)
            {
                if (!destProps.Any(x => x.Name == sourceProp.Name)) continue;

                var p = destProps.First(x => x.Name == sourceProp.Name);

                try
                {
                    p.SetValue(dest, sourceProp.GetValue(source, null), null);
                }
                catch
                {
                    try
                    {
                        p.SetValue(dest, sourceProp.GetValue(source, null).ToString(), null);
                    }
                    catch
                    {
                        //Whatever.
                    }
                }
            }
        }

        public static bool IsBetween<T>(this T item, T start, T end)
        {
            return Comparer<T>.Default.Compare(item, start) >= 0
                   && Comparer<T>.Default.Compare(item, end) <= 0;
        }

        public static string format(this string source, params object[] parms)
        {
            return string.Format(source, parms);
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

        public class GetMethodExtT { }

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