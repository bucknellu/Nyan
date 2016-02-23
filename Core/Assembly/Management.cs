using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Windows.Forms;
using Nyan.Core.Settings;
using Nyan.Core.Shared;
using Message = Nyan.Core.Modules.Log.Message;

namespace Nyan.Core.Assembly
{
    /// <summary>
    ///     Assembly management. At static creation time loads all assemblies placed in the same physical directory as the
    ///     caller project and keep a static reference to them.
    /// </summary>
    public static class Management
    {
        private static readonly Dictionary<string, System.Reflection.Assembly> AssemblyCache = new Dictionary<string, System.Reflection.Assembly>();
        private static readonly Dictionary<Type, List<Type>> InterfaceClassesCache = new Dictionary<Type, List<Type>>();
        private static readonly object Lock = new object();
        private static readonly List<FileSystemWatcher> FsMonitors = new List<FileSystemWatcher>();
        private static readonly List<string> WatchedSources = new List<string>();

        private static readonly Dictionary<string, string> UniqueAssemblies = new Dictionary<string, string>();

        static Management()
        {
#pragma warning disable 618
            AppDomain.CurrentDomain.SetCachePath(Current.DataDirectory + "\\sc");
            AppDomain.CurrentDomain.SetShadowCopyPath(AppDomain.CurrentDomain.BaseDirectory);
            AppDomain.CurrentDomain.SetShadowCopyFiles();
#pragma warning restore 618

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var self = System.Reflection.Assembly.GetEntryAssembly();

            if (self != null)
                AssemblyCache.Add(self.ToString(), self);

            // 1st cycle: Local (base directory) assemblies

            LoadAssembliesFromDirectory(Current.BaseDirectory);

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

                var attr = File.GetAttributes(path);

                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    //It's a directory: Load all assemblies and set up monitor.

                    var assylist = Directory.GetFiles(path, "*.dll");

                    foreach (var dll in assylist)
                        LoadAssemblyFromPath(dll);

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

                    Modules.Log.System.Add("[" + path + "]: Monitoring", Message.EContentType.StartupSequence);
                }
                else
                {
                    //It's a file: Load it directly.
                    LoadAssemblyFromPath(path);
                }
            }
        }

        private static void FileSystemWatcher_OnChanged(object sender, FileSystemEventArgs e)
        {
            if (Process.Sequences.IsShuttingDown) return;

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
            } catch { }
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