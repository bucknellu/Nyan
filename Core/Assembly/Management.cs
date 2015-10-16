using Nyan.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nyan.Core.Assembly
{
    public static class Management
    {
        static Dictionary<string, System.Reflection.Assembly> _assys = new Dictionary<string, System.Reflection.Assembly>();

        static Management()
        {
            LoadAssemblies();
        }

        private static void LoadAssemblies()
        {

            LoadLocalAssemblies();

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

            foreach (var loadedAssembly in loadedAssemblies)
                LoadAssembly(loadedAssembly);
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
                catch 
                {
                }

            }
        }

        private static void LoadAssembly(System.Reflection.Assembly assembly)
        {
            foreach (
                var name in
                    assembly.GetReferencedAssemblies()
                        .Where(name => AppDomain.CurrentDomain.GetAssemblies().All(a => a.FullName != name.FullName)))
                LoadAssembly(System.Reflection.Assembly.Load(name));

            try
            {
                System.Reflection.Assembly.Load(assembly.FullName);
            }
            catch (Exception)
            {
                Settings.Current.Log.Add("Assembly {0} failed to load.".format(assembly.FullName), Modules.Log.Message.EContentType.Warning);
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
                    {
                        ret.Add(item3);
                    }

                }
            }

            return ret;
        }
    }
}
