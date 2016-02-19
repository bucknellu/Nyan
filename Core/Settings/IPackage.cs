using System;
using Nyan.Core.Modules.Identity;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Modules.Encryption;
using Nyan.Core.Modules.Environment;
using Nyan.Core.Modules.Log;

namespace Nyan.Core.Settings
{
    public interface IPackage
    {
        LogProvider Log { get; }
        ICacheProvider Cache { get; }
        IEnvironmentProvider Environment { get; }
        IEncryptionProvider Encryption { get; }
        IAuthorizationProvider Authorization { get; }
        Type GlobalConnectionBundleType { get; }
        string WebApiCORSDomains { get; }
    }
}