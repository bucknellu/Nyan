using Nyan.Core.Modules.Authorization;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Modules.Cache.Memory;
using System;

namespace Nyan.Modules.Data.PgSql
{
    [PackagePriority(Level = -2)]
    public class PgSqlSettingsPackage : IPackage
    {
        public PgSqlSettingsPackage()
        {
            Log = new NullLogProvider();
            Cache = new MemoryCacheProvider();
            Encryption = new NullEncryptionProvider();
            Environment = new DefaultEnvironmentProvider();
            GlobalConnectionBundleType = typeof(PgSqlBundle);
        }

        public ICacheProvider Cache { get; set; }
        public IEncryptionProvider Encryption { get; set; }
        public IAuthorizationProvider Authorization { get; private set; }
        public IEnvironmentProvider Environment { get; set; }
        public Type GlobalConnectionBundleType { get; set; }
        public LogProvider Log { get; set; }
    }
}
