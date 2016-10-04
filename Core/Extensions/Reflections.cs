using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Nyan.Core.Settings;
using Nyan.Core.Wrappers;

namespace Nyan.Core.Extensions
{
    /// <summary>
    /// Reflection-related extensions.
    /// </summary>
    public static class Reflections
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

        /// <summary>
        /// Gets an object fo type T, transposing matching keys from a reference dictionary. Optionally consumes a translation dictionary that maps correlating keys.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict">The reference dictionary.</param>
        /// <param name="translationDictionary">The translation dictionary.</param>
        /// <returns></returns>
        public static T GetObject<T>(this IDictionary<string, object> dict, Dictionary<string, string> translationDictionary = null)
        {
            var type = typeof (T);

            var obj = Activator.CreateInstance(type);

            foreach (var kv in dict)
            {
                var propertyNameRes = kv.Key;

                if (translationDictionary != null)
                    if (translationDictionary.ContainsValue(propertyNameRes))
                        propertyNameRes = translationDictionary.FirstOrDefault(x => x.Value == propertyNameRes).Key;

                var k = type.GetProperty(propertyNameRes, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                var val = kv.Value;

                if (k == null) continue;

                var kt = k.PropertyType;

                if (k.PropertyType.IsPrimitiveType())
                {
                    try
                    {
                        if (val is decimal) val = Convert.ToInt64(val);
                        if (val is short && kt == typeof (bool)) val = (Convert.ToInt16(val) == 1);
                        if (val is long && kt == typeof (string)) val = val.ToString();
                        if (kt == typeof (decimal)) val = Convert.ToDecimal(val);
                        if (kt == typeof (short)) val = Convert.ToInt16(val);
                        if (kt == typeof (int)) val = Convert.ToInt32(val);
                        if (kt == typeof (long)) val = Convert.ToInt64(val);
                        if (kt == typeof (Guid)) if (val != null) val = new Guid(val.ToString());
                        if (kt.IsEnum) val = Enum.Parse(k.PropertyType, val.ToString());

                        k.SetValue(obj, val);
                    } catch (Exception e)
                    {
                        Current.Log.Add(e);
                        throw;
                    }
                }
                else
                    k.SetValue(obj, kv.Value != null ? JsonConvert.DeserializeObject(kv.Value.ToString(), kt) : null);
            }

            return (T) obj;
        }

        /// <summary>
        /// Gets the method ext.
        /// </summary>
        /// <param name="thisType">Type of the this.</param>
        /// <param name="name">The name.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the method ext.
        /// </summary>
        /// <param name="thisType">Type of the this.</param>
        /// <param name="name">The name.</param>
        /// <param name="bindingFlags">The binding flags.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <returns></returns>
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
            foreach (var memberInfo in type.GetMember(name,
                MemberTypes.Method,
                bindingFlags))
            {
                var methodInfo = (MethodInfo) memberInfo;
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

        public static bool IsSimilarType(this Type thisType, Type type)
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
            if (thisType == type || ((thisType.IsGenericParameter || thisType == typeof (GetMethodExtT))
                                     && (type.IsGenericParameter || type == typeof (GetMethodExtT))))
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

        public static dynamic StaticMembersDynamicWrapper(this Type type)
        {
            return new StaticMembersDynamicWrapper(type);
        }

        public static bool IsNumericType(object obj)
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

        public static bool IsPrimitiveType(this Type type)
        {
            return
                type.IsValueType ||
                type.IsPrimitive ||
                PrimitiveTypes.Contains(type) ||
                Convert.GetTypeCode(type) != TypeCode.Object;
        }

        public static T CreateInstance<T>(this Type typeRef)
        {
            try
            {
                return (T) Activator.CreateInstance(typeRef);
            } catch (Exception e)
            {
                throw (e.InnerException.InnerException);
            }
        }

        public static void CopyListPropertiesTo<T, TU>(this IEnumerable<T> source, List<TU> dest)
        {
            dest.Clear();

            foreach (var i in source)
            {
                var uo = (TU)Activator.CreateInstance(typeof(TU), null);

                i.CopyPropertiesTo(uo);
                dest.Add(uo);

            }
        }

        public static void CopyPropertiesTo<T, TU>(this T source, TU dest)
        {
            var sourceProps = typeof (T).GetProperties().Where(x => x.CanRead).ToList();
            var destProps = typeof (TU).GetProperties()
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
                } catch
                {
                    try
                    {
                        p.SetValue(dest, sourceProp.GetValue(source, null).ToString(), null);
                    } catch
                    {
                        //Whatever.
                    }
                }
            }
        }

        public class GetMethodExtT {}
    }
}