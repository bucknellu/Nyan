using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Authorization;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
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
            Environment = refObj.Environment;
            Log = refObj.Log;
            Encryption = refObj.Encryption;
            GlobalConnectionBundleType = refObj.GlobalConnectionBundleType;
            Authorization = refObj.Authorization;

            Log.Add("Nyan - settings initialization: " + refObj.GetType(),
                Message.EContentType.StartupSequence);
            Log.Add("    Cache                     : " + (Cache.ToString() ?? "(none)"),
                Message.EContentType.StartupSequence);
            Log.Add("    Environment               : " + (Environment.ToString() ?? "(none)"),
                Message.EContentType.StartupSequence);
            Log.Add("    Log                       : " + (Log.ToString() ?? "(none)"),
                Message.EContentType.StartupSequence);
            Log.Add("    Encryption                : " + (Encryption.ToString() ?? "(none)"),
                Message.EContentType.StartupSequence);
            Log.Add("    Authorization             : " + (Authorization == null ? "(none)" : Authorization.ToString()),
                Message.EContentType.StartupSequence);
            Log.Add("    GlobalConnectionBundleType: " + (GlobalConnectionBundleType.ToString() ?? "(none)"),
                Message.EContentType.StartupSequence);
        }

        public static ICacheProvider Cache { get; private set; }
        public static IEnvironmentProvider Environment { get; private set; }
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

            //May be unnecessary, since Core always have a NullPackage ready... but hey, no harm done.
            if (!priorityList.Any())
            {
                throw new Exception("(╯°□°）╯︵ ┻━┻ - There are no Nyan Packages included in the project.");
            }

            return (IPackage)Activator.CreateInstance(priorityList[0].Value);
        }
    }
}