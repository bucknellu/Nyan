using Nyan.Core.Modules.Identity;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Scope;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using System;

namespace Nyan.Modules.Data.SqlServer
{
    [PackagePriority(Level = -1)]
    public class SqlServerSettingsPackage : IPackage
    {
        public SqlServerSettingsPackage()
        {
            Log = new NullLogProvider();
            Cache = new Cache.Memory.MemoryCacheProvider();
            Encryption = new NullEncryptionProvider();
            Scope = new DefaultScopeProvider();
            GlobalConnectionBundleType = typeof(SqlServerBundle);
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
