using System;
using System.Collections.Generic;
using System.IO;
using Nyan.Core.Settings;
using Nyan.Core.Shared;

namespace Nyan.Core.Assembly
{
    public static class Management
    {
        private static readonly Dictionary<string, System.Reflection.Assembly> _assys = new Dictionary<string, System.Reflection.Assembly>();

        static Management()
        {
            // This bootstrapper loads all assemblies placed in the same physical directory as the caller project,
            // And keep a static reference to them.

            var self = System.Reflection.Assembly.GetEntryAssembly();

            if (self != null)
                _assys.Add(self.ToString(), self);


            var assylist = Directory.GetFiles(Current.BaseDirectory, "*.dll");

            foreach (var dll in assylist)
            {
                try
                {
                    var assy = System.Reflection.Assembly.LoadFile(dll);

                    if (!_assys.ContainsKey(assy.ToString()))
                        _assys.Add(assy.ToString(), assy);
                }
                catch
                {
                    //Some DLLs may fail to load. That's fine, let's just ignore them.
                }
            }
        }

        public static List<Type> GetClassesByInterface<T>(bool excludeCoreNullDefinitions = true)
        {
            var type = typeof(T);
            var preRet = new List<Type>();
            var ret = new List<Type>();

            foreach (var item in _assys.Values)
            {
                if (excludeCoreNullDefinitions && (item == System.Reflection.Assembly.GetExecutingAssembly())) continue;

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
                    if (item3.IsInterface) continue;

                    if (!type.IsAssignableFrom(item3)) continue;

                    if (type != item3)
                        preRet.Add(item3);
                }
            }


            var priorityList = new List<KeyValuePair<int, Type>>();


            foreach (var item in preRet)
            {
                var level = 0;

                var attrs = item.GetCustomAttributes(typeof(PriorityAttribute), true);

                if (attrs.Length > 0)
                    level = ((PriorityAttribute)attrs[0]).Level;

                priorityList.Add(new KeyValuePair<int, Type>(level, item));
            }



            priorityList.Sort((firstPair, nextPair) => (nextPair.Key - firstPair.Key));

            foreach (var item in priorityList)
            {
                ret.Add(item.Value);
            }

            return ret;
        }
    }
}