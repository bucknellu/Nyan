using System.Security.Principal;

namespace Nyan.Core.Modules.Identity
{
    public interface IAuthorizationProvider
    {
        IPrincipal Principal { get; }
        IIdentity Identity { get; }
        bool CheckPermission(string pCode);
        void Shutdown();
    }
}