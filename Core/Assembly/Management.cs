using System;
using System.Collections.Generic;
using System.IO;

namespace Nyan.Core.Assembly
{
    public static class Management
    {
        static Dictionary<string, System.Reflection.Assembly> _assys = new Dictionary<string, System.Reflection.Assembly>();

        static Management()
        {
            // This bootstrapper loads all assemblies placed in the same physical directory as the caller project,
            // And keep a static reference to them.

            LoadLocalAssemblies();
        }
        private static void LoadLocalAssemblies()
        {
            List<System.Reflection.Assembly> allAssemblies = new List<System.Reflection.Assembly>();
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            foreach (string dll in Directory.GetFiles(path, "*.dll"))
            {
                try
                {
                    var assy = System.Reflection.Assembly.LoadFile(dll);

                    if (!_assys.ContainsKey(assy.ToString()))
                        _assys.Add(assy.ToString(), assy);

                    allAssemblies.Add(assy);

                }
                catch { }

            }
        }

        public static List<Type> GetClassesByInterface<T>()
        {
            var type = typeof(T);

            List<Type> ret = new List<Type>();

            foreach (var item in _assys.Values)
            {
                var preTypes = item.GetTypes();

                foreach (var item3 in preTypes)
                {
                    if (!item3.IsInterface)

                        if (type.IsAssignableFrom(item3))
                            ret.Add(item3);

                }
            }

            return ret;
        }
    }
}
