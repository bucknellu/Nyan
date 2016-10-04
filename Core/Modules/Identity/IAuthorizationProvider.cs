using System.Collections.Generic;
using System.Security.Principal;

namespace Nyan.Core.Modules.Identity
{
    public interface IAuthorizationProvider
    {
        IIdentity Identity { get; }
        string Id { get; }
        string Locator { get; }
        bool CheckPermission(string pCode);
        bool CheckPermission(IEnumerable<string> pCode);
        void Shutdown();
    }
}