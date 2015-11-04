namespace Nyan.Core.Modules.Identity
{
    public class NullAuthorizationProvider : IAuthorizationProvider
    {
        public bool CheckPermission(string pCode)
        {
            return true;
        }
    }
}