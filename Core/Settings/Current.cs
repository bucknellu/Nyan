using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Identity;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Scope;
using Nyan.Core.Modules.Log;

namespace Nyan.Core.Settings
{
    public static class Current
    {
        private static string _baseDirectory;

        static Current()
        {
            var refObj = ResolveSettingsPackage();

            Cache = refObj.Cache;
            Scope = refObj.Scope;
            Log = refObj.Log;
            Encryption = refObj.Encryption;
            GlobalConnectionBundleType = refObj.GlobalConnectionBundleType;
            Authorization = refObj.Authorization;

            Log.Add(@"  |\_/|", Message.EContentType.Warning);
            Log.Add(@" >(o.O)<    Nyan " + System.Reflection.Assembly.GetCallingAssembly().GetName().Version, Message.EContentType.Warning);
            Log.Add(@"  (___)", Message.EContentType.Warning);
            Log.Add(@"   U", Message.EContentType.Warning);
            Log.Add("Settings          : " + refObj.GetType(), Message.EContentType.StartupSequence);
            Log.Add("Cache             : " + (Cache == null ? "(none)" : Cache.ToString()), Message.EContentType.StartupSequence);
            Log.Add("Environment       : " + (Scope == null ? "(none)" : Scope.ToString()), Message.EContentType.StartupSequence);
            Log.Add("Log               : " + (Log == null ? "(none)" : Log.ToString()), Message.EContentType.StartupSequence);
            Log.Add("Encryption        : " + (Encryption == null ? "(none)" : Encryption.ToString()), Message.EContentType.StartupSequence);
            Log.Add("Authorization     : " + (Authorization == null ? "(none)" : Authorization.ToString()), Message.EContentType.StartupSequence);
            Log.Add("Global BundleType : " + (GlobalConnectionBundleType == null ? "(none)" : GlobalConnectionBundleType.ToString()), Message.EContentType.StartupSequence);

            //Post-initialization procedures
            if (Cache != null)
                Cache.Initialize();
        }

        public static ICacheProvider Cache { get; private set; }
        public static IScopeProvider Scope { get; private set; }
        public static IEncryptionProvider Encryption { get; private set; }
        public static IAuthorizationProvider Authorization { get; set; }
        public static LogProvider Log { get; private set; }
        public static Type GlobalConnectionBundleType { get; set; }

        public static string BaseDirectory
        {
            get
            {
                if (_baseDirectory != null) return _baseDirectory;

                _baseDirectory = HostingEnvironment.MapPath("~/bin") ??
                                 Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                return _baseDirectory;
            }

            internal set { _baseDirectory = value; }
        }

        private static IPackage ResolveSettingsPackage()
        {
            var priorityList = new List<KeyValuePair<int, Type>>();

            var packages = Management.GetClassesByInterface<IPackage>();

            foreach (var item in packages)
            {
                var level = 0;

                var attrs = item.GetCustomAttributes(typeof(PackagePriorityAttribute), true);

                if (attrs.Length > 0)
                    level = ((PackagePriorityAttribute)attrs[0]).Level;

                priorityList.Add(new KeyValuePair<int, Type>(level, item));
            }

            priorityList.Sort((firstPair, nextPair) => (nextPair.Key - firstPair.Key));


            if (priorityList.Any()) return (IPackage) Activator.CreateInstance(priorityList[0].Value);

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

                var suthorizationModules = Management.GetClassesByInterface<IAuthorizationProvider>();
                if (suthorizationModules.Any()) package.Authorization = suthorizationModules[0].CreateInstance<IAuthorizationProvider>();
            }
            catch
            {
                //It's OK to ignore errors here.
            }


            return package;
        }
    }
}