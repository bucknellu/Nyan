using Nyan.Core.Modules.Log;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Encryption;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nyan.Core.Settings
{
    public static class Current
    {
        private static string _BaseDirectory = null;

        static Current()
        {
            var refObj = ResolveSettingsPackage();

            Cache = refObj.Cache;
            Environment = refObj.Environment;
            Log = refObj.Log;
            Encryption = refObj.Encryption;
            GlobalConnectionBundleType = refObj.GlobalConnectionBundleType;

        }

        private static IPackage ResolveSettingsPackage()
        {

            var priorityList = new List<KeyValuePair<int, Type>>();

            var packages = Assembly.Management.GetClassesByInterface<IPackage>();

            foreach (var item in packages)
            {
                var level = 0;

                var attrs = item.GetCustomAttributes(typeof(PackagePriorityAttribute), true);

                if (attrs.Length > 0)
                    level = ((PackagePriorityAttribute)attrs[0]).Level;

                priorityList.Add(new KeyValuePair<int, Type>(level, item));
            }

            priorityList.Sort((firstPair, nextPair) => { return (nextPair.Key - firstPair.Key); });

            if (!priorityList.Any())
            {
                throw new Exception("There are no Nyan Packages included in the project. ");
            }

            return (IPackage)Activator.CreateInstance(priorityList[0].Value);

        }

        public static ICacheProvider Cache { get; private set; }

        public static IEnvironmentProvider Environment { get; private set; }

        public static ILogProvider Log { get; private set; }
        public static IEncryptionProvider Encryption { get; private set; }

        public static Type GlobalConnectionBundleType { get; set; }

        public static string BaseDirectory
        {
            get
            {
                if (_BaseDirectory != null) return _BaseDirectory;

                string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string webPath = System.Web.Hosting.HostingEnvironment.MapPath("~/bin");

                _BaseDirectory = webPath != null ? webPath : path;

                return _BaseDirectory;
            }

            internal set
            {
                _BaseDirectory = value;
            }
        }
    }

}