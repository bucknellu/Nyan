using System;
using Nyan.Core.Modules.Authorization;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Log;

namespace Nyan.Core.Settings
{
    [PackagePriority(Level = -2)]
    internal class DefaultSettingsPackage : IPackage
    {
        public DefaultSettingsPackage()
        {
            Log = new NullLogProvider();
            Cache = new NullCache();
            Encryption = new NullEncryptionProvider();
            Environment = new DefaultEnvironmentProvider();
            Authorization = new NullAuthorizationProvider();
            GlobalConnectionBundleType = null;
        }

        public LogProvider Log { get; private set; }
        public ICacheProvider Cache { get; private set; }
        public IEnvironmentProvider Environment { get; private set; }
        public IEncryptionProvider Encryption { get; private set; }
        public IAuthorizationProvider Authorization { get; private set; }
        public Type GlobalConnectionBundleType { get; set; }
    }
}