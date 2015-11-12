namespace Nyan.Core.Modules.Identity
{
    public interface IAuthorizationProvider
    {
        bool CheckPermission(string pCode);
        void Shutdown();
    }
}