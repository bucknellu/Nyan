using System;
using Nyan.Core.Modules.Authorization;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Modules.Cache.Memory;

namespace Nyan.Modules.Data.MySQL
{
    [PackagePriority(Level = -2)]
    // Data adapter package priority (higher than default (-3), but lower than pre-specified packages (-1)
    public class MySqlSettingsPackage : IPackage
    {
        public MySqlSettingsPackage()
        {
            Log = new NullLogProvider();
            Cache = new MemoryCacheProvider();
            Encryption = new NullEncryptionProvider();
            Environment = new DefaultEnvironmentProvider();
            GlobalConnectionBundleType = typeof(MySqlBundle);
        }

        public ICacheProvider Cache { get; set; }
        public IEncryptionProvider Encryption { get; set; }
        public IAuthorizationProvider Authorization { get; private set; }
        public IEnvironmentProvider Environment { get; set; }
        public Type GlobalConnectionBundleType { get; set; }
        public LogProvider Log { get; set; }
    }
}