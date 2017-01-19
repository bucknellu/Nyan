using System;
using System.Linq;
using System.Windows.Forms;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Data.Connection;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Identity;
using Nyan.Core.Modules.Log;
using Nyan.Core.Process;
using Message = Nyan.Core.Modules.Log.Message;

namespace Nyan.Core.Settings
{
    public static class Current
    {
        static Current()
        {
            try { Application.ApplicationExit += Application_ApplicationExit; } catch { }

            try
            {
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                //AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            }
            catch { }

            var refObj = ResolveSettingsPackage();

            Cache = refObj.Cache;
            Environment = refObj.Environment;
            Log = refObj.Log;
            Encryption = refObj.Encryption;
            GlobalConnectionBundleType = refObj.GlobalConnectionBundleType;
            Authorization = refObj.Authorization;
            WebApiCORSDomains = refObj.WebApiCORSDomains;

            Log.Add(@"   |\_/|          |", Message.EContentType.Info);
            Log.Add(@"  >(o.O)<         | Nyan " + System.Reflection.Assembly.GetCallingAssembly().GetName().Version, Message.EContentType.Info);
            Log.Add(@"  c(___)          |", Message.EContentType.Info);

            Log.Add("Settings          : " + refObj.GetType(), Message.EContentType.StartupSequence);

            Log.Add("Cache             : " + (Cache == null ? "(none)" : Cache.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Environment       : " + (Environment == null ? "(none)" : Environment.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Log               : " + (Log == null ? "(none)" : Log.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Encryption        : " + (Encryption == null ? "(none)" : Encryption.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Authorization     : " + (Authorization == null ? "(none)" : Authorization.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Global BundleType : " + (GlobalConnectionBundleType == null ? "(none)" : GlobalConnectionBundleType.ToString()), Message.EContentType.MoreInfo);
            Log.Add("Application       : " + Configuration.ApplicationAssemblyName, Message.EContentType.MoreInfo);
            Log.Add("App Location      : " + Configuration.BaseDirectory, Message.EContentType.MoreInfo);
            Log.Add("App Data          : " + Configuration.DataDirectory, Message.EContentType.MoreInfo);

            Log.Add("Stack status      : Operational", Message.EContentType.StartupSequence);

            //Post-initialization procedures
            if (Cache != null) Cache.Initialize();

            Sequences.Start();
        }

        public static ICacheProvider Cache { get; }
        public static IEnvironmentProvider Environment { get; }
        public static IEncryptionProvider Encryption { get; }
        public static IAuthorizationProvider Authorization { get; }
        public static LogProvider Log { get; }
        public static Type GlobalConnectionBundleType { get; }

        public static string WebApiCORSDomains { get; private set; }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Add((Exception)e.ExceptionObject);
            Sequences.End("Unhandled Exception");
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e) { Sequences.End("Process Exit"); }

        private static void Application_ApplicationExit(object sender, EventArgs e) { Sequences.End("Application Exit"); }

        private static IPackage ResolveSettingsPackage()
        {
            var packages = Management.GetClassesByInterface<IPackage>();

            if (packages.Any()) return (IPackage)Activator.CreateInstance(packages[0]);

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

                var environmentModules = Management.GetClassesByInterface<IEnvironmentProvider>();
                if (environmentModules.Any()) package.Environment = environmentModules[0].CreateInstance<IEnvironmentProvider>();

                var authorizationModules = Management.GetClassesByInterface<IAuthorizationProvider>();
                if (authorizationModules.Any()) package.Authorization = authorizationModules[0].CreateInstance<IAuthorizationProvider>();

                var connectionBundles = Management.GetClassesByInterface<ConnectionBundlePrimitive>();
                if (connectionBundles.Any()) package.GlobalConnectionBundleType = connectionBundles[0];
            }
            catch (Exception e)
            {
                //It's OK to ignore errors here.
            }

            return package;
        }
    }
}