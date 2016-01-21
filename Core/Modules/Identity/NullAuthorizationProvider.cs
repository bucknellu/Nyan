using System;
using System.Security.Principal;

namespace Nyan.Core.Modules.Identity
{
    public class NullAuthorizationProvider : IAuthorizationProvider
    {
        public IPrincipal Principal { get { return null; } }
        public IIdentity Identity { get { return null; } }
        public string Id { get { return null; } }
        public string Locator { get { return null; } }
        public bool CheckPermission(string pCode) { return true; }
        public void Shutdown() { }
    }
}