using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Data.Connection;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Log;
using System;

namespace Nyan.Core.Settings
{
    public interface IPackage
    {
        ILogProvider Log { get; }
        ICacheProvider Cache { get; }
        IEnvironmentProvider Environment { get; }
        IEncryptionProvider Encryption { get; }
        Type GlobalConnectionBundleType { get; }
    }
}
