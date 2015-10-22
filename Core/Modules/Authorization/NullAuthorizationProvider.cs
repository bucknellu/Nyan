namespace Nyan.Core.Modules.Authorization
{
    public class NullAuthorizationProvider : IAuthorizationProvider
    {
        public bool CheckPermission(string pCode)
        {
            return true;
        }
    }
}