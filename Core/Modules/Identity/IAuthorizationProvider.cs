using System.Security.Principal;

namespace Nyan.Core.Modules.Identity
{
    public interface IAuthorizationProvider
    {
        IIdentity Identity { get; }
        bool CheckPermission(string pCode);
        void Shutdown();
    }
}