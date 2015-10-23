using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Modules.Cache.Memory;
using System;

namespace Nyan.Modules.Data.MySQL
{
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
        public IEnvironmentProvider Environment { get; set; }
        public Type GlobalConnectionBundleType { get; set; }
        public ILogProvider Log { get; set; }
    }
}
