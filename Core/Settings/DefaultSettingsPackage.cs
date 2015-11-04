using System;
using Nyan.Core.Modules.Identity;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Scope;
using Nyan.Core.Modules.Log;

namespace Nyan.Core.Settings
{
    [PackagePriority(Level = -3)]
    internal class DefaultSettingsPackage : IPackage
    {
        public DefaultSettingsPackage()
        {
            Log = new NullLogProvider();
            Cache = new NullCacheProvider();
            Encryption = new NullEncryptionProvider();
            Scope = new DefaultScopeProvider();
            Authorization = new NullAuthorizationProvider();
            GlobalConnectionBundleType = null;
        }

        public LogProvider Log { get; internal set; }
        public ICacheProvider Cache { get; internal set; }
        public IScopeProvider Scope { get; internal set; }
        public IEncryptionProvider Encryption { get; internal set; }
        public IAuthorizationProvider Authorization { get; internal set; }
        public Type GlobalConnectionBundleType { get; internal set; }
    }
}