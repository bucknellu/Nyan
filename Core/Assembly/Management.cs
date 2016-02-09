using System;
using System.Collections.Generic;
using System.IO;
using Nyan.Core.Settings;
using Nyan.Core.Shared;
using System.Reflection;
using System.Linq;

namespace Nyan.Core.Assembly
{
    /// <summary>
    /// Assembly management. At static creation time loads all assemblies placed in the same physical directory as the caller project and keep a static reference to them.
    /// </summary>
    public static class Management
    {
        private static readonly Dictionary<string, System.Reflection.Assembly> _assys = new Dictionary<string, System.Reflection.Assembly>();

        static Management()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var self = System.Reflection.Assembly.GetEntryAssembly();

            if (self != null)
                _assys.Add(self.ToString(), self);

            // 1st cycle: Local (base directory) assemblies

            loadAssembliesFromDirectory(Current.BaseDirectory);

            //2nd cycle: Directories/assemblies referenced by system

            //    First by process-specific variables...
            loadAssembliesFromDirectory(Environment.GetEnvironmentVariable("nyan_ac_net40", EnvironmentVariableTarget.Process));
            //    then by user-specific variables...
            loadAssembliesFromDirectory(Environment.GetEnvironmentVariable("nyan_ac_net40", EnvironmentVariableTarget.User));
            //    and finally system-wide variables.
            loadAssembliesFromDirectory(Environment.GetEnvironmentVariable("nyan_ac_net40", EnvironmentVariableTarget.Machine));

            //Now try to load:

            int _lastErrCount = -1;
            int _errCount = 0;

            while (_errCount != _lastErrCount)
            {
                _lastErrCount = _errCount;

                foreach (var item in _assys)
                {
                    Modules.Log.System.Add("   LOAD " + item.Value.ToString().Split(',')[0], Modules.Log.Message.EContentType.Info);

                    _errCount = 0;

                    try
                    {
                        var i = item.Value.GetTypes();
                    }
                    catch (Exception)
                    {
                        Modules.Log.System.Add("    ERR " + item.Value.ToString(), Modules.Log.Message.EContentType.Info);
                        _errCount++;
                    }
                }

                Modules.Log.System.Add("    Previous " + _lastErrCount + ", current " + _errCount + " errors", Modules.Log.Message.EContentType.Info);

            }
            Modules.Log.System.Add("Loaded modules: ", Modules.Log.Message.EContentType.Info);

            foreach (var item in _assys)
                Modules.Log.System.Add("    " + item.Value.Location + " (" + item.Value.ToString() + ")", Modules.Log.Message.EContentType.Info);

            Modules.Log.System.Add("Fatal Errors: " + _errCount, Modules.Log.Message.EContentType.Info);
        }

        private static System.Reflection.Assembly GetAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().
                   SingleOrDefault(assembly => assembly.GetName().Name == name);
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {


            Modules.Log.System.Add("        " + args.RequestingAssembly.FullName + ": Resolution request");
            Modules.Log.System.Add("            Resolving " + args.Name);
            var shortName = args.Name.Split(',')[0];
            var name = new AssemblyName();
            var probe = GetAssemblyByName(shortName);

            if (probe == null)
                Modules.Log.System.Add("            ERROR");
            else
                Modules.Log.System.Add("            OK      : " + probe.ToString());
            return probe;
        }

        private static void loadAssembliesFromDirectory(string path)
        {
            if (path == null) return;


            if (path.IndexOf(";") > -1) //Semicolon-split list. Parse and process one by one.
            {
                var list = path.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var item in list)
                {
                    loadAssembliesFromDirectory(item);
                }
            }
            else {


                Modules.Log.System.Add("Loading assemblies from [" + path + "]", Modules.Log.Message.EContentType.StartupSequence);

                FileAttributes attr = File.GetAttributes(path);

                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    //It's a directory: Load all assemblies.

                    var assylist = Directory.GetFiles(path, "*.dll");

                    foreach (var dll in assylist)
                    {
                        loadAssemblyFromPath(dll);
                    }
                }
                else
                {
                    //It's a file: Load it directly.
                    loadAssemblyFromPath(path);
                }
            }
        }

        private static void loadAssemblyFromPath(string path)
        {
            if (path == null) return;

            try
            {
                var assy = System.Reflection.Assembly.LoadFile(path);

                if (!_assys.ContainsKey(assy.ToString()))
                    _assys.Add(assy.ToString(), assy);
            }
            catch (Exception e)
            {
                if (e is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = e as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions.ToList();

                    if (loaderExceptions.Count > 0)
                        Modules.Log.System.Add("    Fail " + path + ": " + loaderExceptions[0].Message);
                    else

                        Modules.Log.System.Add("    Fail " + path + ": Undefined.");

                }
                else
                    Modules.Log.System.Add("    Fail " + path + ": " + e.Message);

            }
        }

        /// <summary>
        /// Gets a list of classes by implemented interface/base class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="excludeCoreNullDefinitions">if set to <c>true</c> it ignores all core null providers, retuning only external providers.</param>
        /// <returns>The list of classes.</returns>
        public static List<Type> GetClassesByInterface<T>(bool excludeCoreNullDefinitions = true)
        {
            var type = typeof(T);
            var preRet = new List<Type>();
            var ret = new List<Type>();

            Modules.Log.System.Add("Scanning for " + type.ToString());

            foreach (var item in _assys.Values)
            {
                if (excludeCoreNullDefinitions && (item == System.Reflection.Assembly.GetExecutingAssembly())) continue;

                Type[] preTypes;

                try
                {
                    preTypes = item.GetTypes();
                }
                catch (Exception e)
                {

                    if (e is System.Reflection.ReflectionTypeLoadException)
                    {
                        var typeLoadException = e as ReflectionTypeLoadException;
                        var loaderExceptions = typeLoadException.LoaderExceptions.ToList();

                        if (loaderExceptions.Count > 0)
                            Modules.Log.System.Add("    Fail " + item.ToString() + ": " + loaderExceptions[0].Message);
                        else

                            Modules.Log.System.Add("    Fail " + item.ToString() + ": Undefined.");

                    }
                    else
                        Modules.Log.System.Add("    Fail " + item.ToString() + ": " + e.Message);
                    // Well, this loading can fail by a (long) variety of reasons. 
                    // It's not a real problem not to catch exceptions here. 
                    continue;
                }

                foreach (var item3 in preTypes)
                {
                    if (item3.IsInterface) continue;
                    if (!type.IsAssignableFrom(item3)) continue;
                    if (type != item3) preRet.Add(item3);
                }
            }

            var priorityList = new List<KeyValuePair<int, Type>>();

            Modules.Log.System.Add("    " + preRet.Count + " items");


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