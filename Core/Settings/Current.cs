using System;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Windows.Forms;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Data.Connection;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Identity;
using Nyan.Core.Modules.Log;
using Nyan.Core.Modules.Scope;
using Nyan.Core.Process;
using Message = Nyan.Core.Modules.Log.Message;

namespace Nyan.Core.Settings
{
    public static class Current
    {
        private static string _baseDirectory;
        private static string _dataDirectory;

        static Current()
        {
            try { Application.ApplicationExit += Application_ApplicationExit; } catch {}

            try
            {
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                //AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            } catch {}

            var refObj = ResolveSettingsPackage();

            Cache = refObj.Cache;
            Scope = refObj.Scope;
            Log = refObj.Log;
            Encryption = refObj.Encryption;
            GlobalConnectionBundleType = refObj.GlobalConnectionBundleType;
            Authorization = refObj.Authorization;

            Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString();
            Host = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            Log.Add(@"   |\_/|", Message.EContentType.Info);
            Log.Add(@"  >(o.O)<           Nyan " + System.Reflection.Assembly.GetCallingAssembly().GetName().Version, Message.EContentType.Info);
            Log.Add(@"  c(___)", Message.EContentType.Info);

            Log.Add("Settings          : " + refObj.GetType(), Message.EContentType.StartupSequence);

            Log.Add("Cache             : " + (Cache == null ? "(none)" : Cache.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Environment       : " + (Scope == null ? "(none)" : Scope.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Log               : " + (Log == null ? "(none)" : Log.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Encryption        : " + (Encryption == null ? "(none)" : Encryption.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Authorization     : " + (Authorization == null ? "(none)" : Authorization.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Global BundleType : " + (GlobalConnectionBundleType == null ? "(none)" : GlobalConnectionBundleType.ToString()), Message.EContentType.MoreInfo);
            Log.Add("App Location      : " + BaseDirectory, Message.EContentType.MoreInfo);
            Log.Add("App Data          : " + DataDirectory, Message.EContentType.MoreInfo);

            Log.Add("Stack status      : Operational", Message.EContentType.StartupSequence);

            //Post-initialization procedures
            if (Cache != null)
                Cache.Initialize();
        }

        public static ICacheProvider Cache { get; private set; }
        public static IScopeProvider Scope { get; private set; }
        public static IEncryptionProvider Encryption { get; private set; }
        public static IAuthorizationProvider Authorization { get; private set; }
        public static LogProvider Log { get; private set; }
        public static Type GlobalConnectionBundleType { get; private set; }
        public static string Version { get; private set; }
        public static string Host { get; private set; }

        public static string BaseDirectory
        {
            get
            {
                if (_baseDirectory != null) return _baseDirectory;

                _baseDirectory = HostingEnvironment.MapPath("~/bin") ?? Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                return _baseDirectory;
            }

            internal set { _baseDirectory = value; }
        }

        public static string DataDirectory
        {
            get
            {
                if (_dataDirectory != null) return _dataDirectory;

                _dataDirectory = HostingEnvironment.MapPath("~/App_Data");

                if (_dataDirectory != null)
                {
                    try
                    {
                        if (!Directory.Exists(_dataDirectory))
                            Directory.CreateDirectory(_dataDirectory);
                    } catch
                    {
                        _dataDirectory = null;
                    }
                }

                if (_dataDirectory == null) _dataDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                return _dataDirectory;
            }

            internal set { _dataDirectory = value; }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Add((Exception) e.ExceptionObject);
            Sequences.End("Unhandled Exception");
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e) { Sequences.End("Process Exit"); }

        private static void Application_ApplicationExit(object sender, EventArgs e) { Sequences.End("Application Exit"); }

        private static IPackage ResolveSettingsPackage()
        {
            var packages = Management.GetClassesByInterface<IPackage>();

            if (packages.Any()) return (IPackage) Activator.CreateInstance(packages[0]);

            //No package defined? not to worry; let's create one with the provided pieces.

            var package = new DefaultSettingsPackage();

            try
            {
                var logModules = Management.GetClassesByInterface<LogProvider>();
                if (logModules.Any()) package.Log = logModules[0].CreateInstance<LogProvider>();

                var cacheModules = Management.GetClassesByInterface<ICacheProvider>();
                if (cacheModules.Any()) package.Cache = cacheModules[0].CreateInstance<ICacheProvider>();

                var encryptionModules = Management.GetClassesByInterface<IEncryptionProvider>();
                if (encryptionModules.Any()) package.Encryption = encryptionModules[0].CreateInstance<IEncryptionProvider>();

                var scopeModules = Management.GetClassesByInterface<IScopeProvider>();
                if (scopeModules.Any()) package.Scope = scopeModules[0].CreateInstance<IScopeProvider>();

                var authorizationModules = Management.GetClassesByInterface<IAuthorizationProvider>();
                if (authorizationModules.Any()) package.Authorization = authorizationModules[0].CreateInstance<IAuthorizationProvider>();

                var connectionBundles = Management.GetClassesByInterface<ConnectionBundlePrimitive>();
                if (connectionBundles.Any()) package.GlobalConnectionBundleType = connectionBundles[0];
            } catch (Exception e)
            {
                //It's OK to ignore errors here.
            }

            return package;
        }
    }
}