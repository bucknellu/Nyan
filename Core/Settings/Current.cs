using Nyan.Core.Modules.Log;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Data.Connection;
using System;
using System.Collections.Generic;

namespace Nyan.Core.Settings
{
    public static class Current
    {
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

            return (IPackage)Activator.CreateInstance(priorityList[0].Value);

        }

        public static ICacheProvider Cache { get; private set; }

        public static IEnvironmentProvider Environment { get; private set; }

        public static ILogProvider Log { get; private set; }
        public static IEncryptionProvider Encryption { get; private set; }

        public static Type GlobalConnectionBundleType { get; set; }

    }

}