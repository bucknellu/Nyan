namespace Nyan.Core.Modules.Authorization
{
    public interface IAuthorizationProvider
    {
        bool CheckPermission(string pCode);
    }
}