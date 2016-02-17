using System;
using Nyan.Core.Modules.Identity;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Log;
using Nyan.Core.Shared;

namespace Nyan.Core.Settings
{
    [Priority(Level = -3)]
    internal class DefaultSettingsPackage : IPackage
    {
        public DefaultSettingsPackage()
        {
            Log = new NullLogProvider();
            Cache = new NullCacheProvider();
            Encryption = new NullEncryptionProvider();
            Environment = new DefaultEnvironmentProvider();
            Authorization = new NullAuthorizationProvider();
            GlobalConnectionBundleType = null;
        }

        public LogProvider Log { get; internal set; }
        public ICacheProvider Cache { get; internal set; }
        public IEnvironmentProvider Environment { get; internal set; }
        public IEncryptionProvider Encryption { get; internal set; }
        public IAuthorizationProvider Authorization { get; internal set; }
        public Type GlobalConnectionBundleType { get; internal set; }
    }
}