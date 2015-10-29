using Nyan.Core.Modules.Authorization;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Log;
using Nyan.Core.Modules.Scope;
using Nyan.Core.Settings;
using Nyan.Modules.Cache.Memory;
using System;

namespace Nyan.Modules.Data.MongoDb
{
    [PackagePriority(Level = -2)]
    // Data adapter package priority (higher than default (-3), but lower than pre-specified packages (-1)
    public class MongoDbSettingsPackage : IPackage
    {
        public MongoDbSettingsPackage()
        {
            Log = new NullLogProvider();
            Cache = new MemoryCacheProvider();
            Encryption = new NullEncryptionProvider();
            Environment = new DefaultEnvironmentProvider();
            GlobalConnectionBundleType = typeof(MongoDbBundle);
        }

        public ICacheProvider Cache { get; set; }
        public IEncryptionProvider Encryption { get; set; }
        public IAuthorizationProvider Authorization { get; private set; }
        public IEnvironmentProvider Environment { get; set; }
        public Type GlobalConnectionBundleType { get; set; }
        public LogProvider Log { get; set; }
    }
}
