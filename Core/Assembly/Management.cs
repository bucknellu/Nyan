using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using Nyan.Core.Modules.Log;
using Nyan.Core.Process;
using Nyan.Core.Settings;
using Nyan.Core.Shared;

namespace Nyan.Core.Assembly
{
    /// <summary>
    ///     Assembly management. At static creation time loads all assemblies placed in the same physical directory as the
    ///     caller project and keep a static reference to them.
    /// </summary>
    public static class Management
    {
        public static readonly Dictionary<string, System.Reflection.Assembly> AssemblyCache = new Dictionary<string, System.Reflection.Assembly>();
        private static readonly Dictionary<Type, List<Type>> InterfaceClassesCache = new Dictionary<Type, List<Type>>();
        private static readonly object Lock = new object();
        private static readonly List<FileSystemWatcher> FsMonitors = new List<FileSystemWatcher>();
        private static readonly List<string> WatchedSources = new List<string>();
        public static readonly Dictionary<string, string> UniqueAssemblies = new Dictionary<string, string>();

        static Management()
        {
#pragma warning disable 618
            AppDomain.CurrentDomain.SetShadowCopyFiles();

            var targetScDir = Configuration.DataDirectory + "\\sc";

            if (!Directory.Exists(targetScDir))
                Directory.CreateDirectory(targetScDir);

            AppDomain.CurrentDomain.SetCachePath(Configuration.DataDirectory + "\\sc");

#pragma warning restore 618

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var self = System.Reflection.Assembly.GetEntryAssembly();

            if (self != null)
                AssemblyCache.Add(self.ToString(), self);

            // 1st cycle: Local (base directory) assemblies

            LoadAssembliesFromDirectory(Configuration.BaseDirectory);

            //2nd cycle: Directories/assemblies referenced by system

            //    First by process-specific variables...
            LoadAssembliesFromDirectory(Environment.GetEnvironmentVariable("nyan_ac_net40", EnvironmentVariableTarget.Process));
            //    then by user-specific variables...
            LoadAssembliesFromDirectory(Environment.GetEnvironmentVariable("nyan_ac_net40", EnvironmentVariableTarget.User));
            //    and finally system-wide variables.
            LoadAssembliesFromDirectory(Environment.GetEnvironmentVariable("nyan_ac_net40", EnvironmentVariableTarget.Machine));

            //Now try to load:

            var lastErrCount = -1;
            var errCount = 0;

            while (errCount != lastErrCount)
            {
                lastErrCount = errCount;

                foreach (var item in AssemblyCache)
                {
                    Modules.Log.System.Add("   LOAD " + item.Value.ToString().Split(',')[0]);

                    errCount = 0;

                    try
                    {
                        item.Value.GetTypes();
                    }
                    catch (Exception)
                    {
                        Modules.Log.System.Add("    ERR " + item.Value);
                        errCount++;
                    }
                }

                Modules.Log.System.Add("    Previous " + lastErrCount + ", current " + errCount + " errors");
            }
            Modules.Log.System.Add("Loaded modules: ");

            foreach (var item in AssemblyCache)
                Modules.Log.System.Add("    " + item.Value.Location + " (" + item.Value + ")");

            Modules.Log.System.Add("Fatal Errors: " + errCount);
        }

        private static System.Reflection.Assembly GetAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().
                SingleOrDefault(assembly => assembly.GetName().Name == name);
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.RequestingAssembly != null)
                Modules.Log.System.Add("        " + args.RequestingAssembly.FullName + ": Resolution request");

            Modules.Log.System.Add("            Resolving " + args.Name);
            var shortName = args.Name.Split(',')[0];

            var probe = GetAssemblyByName(shortName);

            if (probe == null)
                Modules.Log.System.Add("            ERROR");
            else
                Modules.Log.System.Add("            OK      : " + probe);
            return probe;
        }

        private static void LoadAssembliesFromDirectory(string path)
        {
            if (path == null) return;

            if (path.IndexOf(";", StringComparison.Ordinal) > -1) //Semicolon-split list. Parse and process one by one.
            {
                var list = path.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var item in list)
                    LoadAssembliesFromDirectory(item);
            }
            else
            {
                if (WatchedSources.Contains(path)) return;
                WatchedSources.Add(path);

                AppDomain.CurrentDomain.SetShadowCopyPath(path);

                var attr = File.GetAttributes(path);

                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    //It's a directory: Load all assemblies and set up monitor.

                    var assylist = Directory.GetFiles(path, "*.dll");

                    foreach (var dll in assylist) LoadAssemblyFromPath(dll);

                    var watcher = new FileSystemWatcher
                    {
                        Path = path,
                        IncludeSubdirectories = false,
                        NotifyFilter = NotifyFilters.LastWrite,
                        Filter = "*.*"
                    };
                    watcher.Changed += FileSystemWatcher_OnChanged;
                    watcher.EnableRaisingEvents = true;

                    FsMonitors.Add(watcher);

                    Modules.Log.System.Add("Monitoring [" + path + "]", Message.EContentType.StartupSequence);
                }
                else
                {
                    //It's a file: Load it directly.
                    LoadAssemblyFromPath(path);
                }
            }
        }

        static List<string> MonitorWhiteList = new List<string>()
        {
            "InstallUtil.InstallLog",
            "*.InstallLog",
            "*.InstallState",
        };

        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }


        private static void FileSystemWatcher_OnChanged(object sender, FileSystemEventArgs e)
        {
            if (Sequences.IsShuttingDown) return;

            var name = Path.GetFileName(e.FullPath);


            foreach (var i in MonitorWhiteList)
            {
                if (i.IndexOf("*", StringComparison.Ordinal) != -1)
                {
                    var match = i.Replace("*.", "");

                    //TODO: Improve wildcard detection
                    if (name.IndexOf(match) > -1)
                    {
                        return;
                    }

                }
                else if (i.Equals(name))
                {
                    return;
                }
            }

            // No need for system monitors anymore, better to interrupt and dispose all of them.
            foreach (var i in FsMonitors)
            {
                i.EnableRaisingEvents = false;
                i.Changed -= FileSystemWatcher_OnChanged;
                i.Dispose();
            }

            FsMonitors.Clear();

            Current.Log.UseScheduler = false;
            Current.Log.Add("[" + e.FullPath + "]: Change detected", Message.EContentType.ShutdownSequence);
            Modules.Log.System.Add("[" + e.FullPath + "]: Change detected", Message.EContentType.ShutdownSequence);

            //For Web apps
            try { HttpRuntime.UnloadAppDomain(); } catch { }

            //For WinForm apps
            try
            {
                //Application.Restart(); Environment.Exit(0);
            }
            catch { }
        }

        private static void LoadAssemblyFromPath(string path)
        {
            if (path == null) return;

            try
            {
                var p = Path.GetFileName(path);

                if (UniqueAssemblies.ContainsKey(p)) return;

                UniqueAssemblies.Add(p, path);

                var assy = System.Reflection.Assembly.LoadFrom(path);
                if (!AssemblyCache.ContainsKey(assy.ToString()))
                    AssemblyCache.Add(assy.ToString(), assy);
            }
            catch (Exception e)
            {
                var exception = e as ReflectionTypeLoadException;

                if (exception != null)
                {
                    var typeLoadException = exception;
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

        public static List<Type> GetClassesByBaseClass(Type refType, bool limitToMainAssembly = false)
        {
            var classCol = new List<Type>();

            var assySource = new List<System.Reflection.Assembly>();

            if (limitToMainAssembly)
                assySource.Add(Configuration.ApplicationAssembly);
            else
                assySource = AssemblyCache.Values.ToList();

            foreach (var asy in assySource)
            {
                classCol.AddRange(asy
                    .GetTypes()
                    .Where(type => type.BaseType != null)
                    .Where(type => (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == refType) || type.BaseType == refType));
            }

            return classCol;
        }

        public static readonly Dictionary<Type, List<Type>> GetGenericsByBaseClassCache = new Dictionary<Type, List<Type>>();

        public static List<Type> GetGenericsByBaseClass(Type refType)
        {
            if (GetGenericsByBaseClassCache.ContainsKey(refType)) return GetGenericsByBaseClassCache[refType];

            var classCol = new List<Type>();

            try
            {
                foreach (var asy in AssemblyCache.Values.ToList())
                {
                    foreach (var st in asy.GetTypes())
                    {
                        if (st.BaseType == null) continue;
                        if (!st.BaseType.IsGenericType) continue;
                        if (st == refType) continue;

                        try
                        {
                            foreach (var gta in st.BaseType.GenericTypeArguments)
                            {
                                if (gta == refType) classCol.Add(st);
                            }
                        }
                        catch { }
                    }
                }

                GetGenericsByBaseClassCache.Add(refType, classCol);

            }
            catch (Exception e)
            {
                Current.Log.Add(e);
            }

            return classCol;
        }

        /// <summary>
        ///     Gets a list of classes by implemented interface/base class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="excludeCoreNullDefinitions">
        ///     if set to <c>true</c> it ignores all core null providers, retuning only
        ///     external providers.
        /// </param>
        /// <returns>The list of classes.</returns>
        public static List<Type> GetClassesByInterface<T>(bool excludeCoreNullDefinitions = true)
        {
            lock (Lock)
            {
                var type = typeof(T);
                var preRet = new List<Type>();

                if (InterfaceClassesCache.ContainsKey(type))
                    return InterfaceClassesCache[type];

                Modules.Log.System.Add("Scanning for " + type);

                foreach (var item in AssemblyCache.Values)
                {
                    if (excludeCoreNullDefinitions && (item == System.Reflection.Assembly.GetExecutingAssembly())) continue;

                    Type[] preTypes;

                    try
                    {
                        preTypes = item.GetTypes();
                    }
                    catch (Exception e)
                    {
                        if (e is ReflectionTypeLoadException)
                        {
                            var typeLoadException = e as ReflectionTypeLoadException;
                            var loaderExceptions = typeLoadException.LoaderExceptions.ToList();

                            if (loaderExceptions.Count > 0)
                                Modules.Log.System.Add("    Fail " + item + ": " + loaderExceptions[0].Message);
                            else

                                Modules.Log.System.Add("    Fail " + item + ": Undefined.");
                        }
                        else
                            Modules.Log.System.Add("    Fail " + item + ": " + e.Message);
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

                Modules.Log.System.Add("    " + preRet.Count + " [" + type + "] items");

                foreach (var item in preRet)
                {
                    var level = 0;

                    var attrs = item.GetCustomAttributes(typeof(PriorityAttribute), true);

                    if (attrs.Length > 0)
                        level = ((PriorityAttribute)attrs[0]).Level;

                    priorityList.Add(new KeyValuePair<int, Type>(level, item));
                }

                priorityList.Sort((firstPair, nextPair) => nextPair.Key - firstPair.Key);

                var ret = priorityList.Select(item => item.Value).ToList();

                InterfaceClassesCache.Add(type, ret); // Caching results, so similar queries will return from cache

                return ret;
            }
        }
    }
}