using System;
using System.Collections.Generic;
using System.IO;
using Nyan.Core.Settings;

namespace Nyan.Core.Assembly
{
    public static class Management
    {
        private static readonly Dictionary<string, System.Reflection.Assembly> _assys =
            new Dictionary<string, System.Reflection.Assembly>();

        static Management()
        {
            // This bootstrapper loads all assemblies placed in the same physical directory as the caller project,
            // And keep a static reference to them.

            LoadLocalAssemblies();
        }

        private static void LoadLocalAssemblies()
        {
            var allAssemblies = new List<System.Reflection.Assembly>();

            var assylist = Directory.GetFiles(Current.BaseDirectory, "*.dll");

            foreach (var dll in assylist)
            {
                try
                {
                    var assy = System.Reflection.Assembly.LoadFile(dll);

                    if (!_assys.ContainsKey(assy.ToString()))
                        _assys.Add(assy.ToString(), assy);

                    allAssemblies.Add(assy);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<Type> GetClassesByInterface<T>()
        {
            var type = typeof (T);
            var ret = new List<Type>();

            foreach (var item in _assys.Values)
            {
                Type[] preTypes;

                try
                {
                    preTypes = item.GetTypes();
                }
                catch
                {
                    // Well, this loading can fail by a (long) variety of reasons. 
                    // It's not a real problem not to catch exceptions here. 
                    continue;
                }

                foreach (var item3 in preTypes)
                {
                    if (!item3.IsInterface)
                    {
                        if (type.IsAssignableFrom(item3))
                        {
                            ret.Add(item3);
                        }
                    }
                }
            }

            return ret;
        }
    }
}