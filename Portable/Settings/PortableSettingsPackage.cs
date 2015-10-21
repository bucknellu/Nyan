using Nyan.Core.Settings;
using System;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Log;

namespace Nyan.Portable.Settings
{
    [PackagePriority(Level = -1)]
    public class PortableSettingsPackage : IPackage
    {
        public PortableSettingsPackage()
        {
            Log = new NullLogProvider();
            Cache = new Modules.Cache.Memory.MemoryCacheProvider();
            Encryption = new NullEncryptionProvider();
            Environment = new DefaultEnvironmentProvider();
            GlobalConnectionBundleType = typeof(Modules.Data.SQLite.SQLiteBundle);
        }
        public ICacheProvider Cache { get; set; }
        public IEncryptionProvider Encryption { get; set; }
        public IEnvironmentProvider Environment { get; set; }
        public Type GlobalConnectionBundleType { get; set; }
        public ILogProvider Log { get; set; }
    }
}
