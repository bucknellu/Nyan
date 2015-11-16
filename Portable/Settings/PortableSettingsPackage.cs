using Nyan.Core.Modules.Identity;
using Nyan.Core.Settings;
using System;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Scope;
using Nyan.Core.Modules.Log;
using Nyan.Modules.Log.ZeroMQ;
using Nyan.Core.Shared;

namespace Nyan.Portable.Settings
{
    [Priority(Level = -1)]
    public class PortableSettingsPackage : IPackage
    {
        public PortableSettingsPackage()
        {
            Log = new ZeroMqLogProvider();
            Cache = new Modules.Cache.Memory.MemoryCacheProvider();
            Encryption = new NullEncryptionProvider();
            Scope = new DefaultScopeProvider();
            GlobalConnectionBundleType = typeof(Modules.Data.SQLite.SqLiteBundle);
            Authorization = new NullAuthorizationProvider();
        }
        public ICacheProvider Cache { get; set; }
        public IEncryptionProvider Encryption { get; set; }
        public IAuthorizationProvider Authorization { get; private set; }
        public IScopeProvider Scope { get; set; }
        public Type GlobalConnectionBundleType { get; set; }
        public LogProvider Log { get; set; }
    }
}
