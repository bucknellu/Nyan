using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace Nyan.Core.Modules.Identity
{
    public class NullAuthorizationProvider : IAuthorizationProvider
    {
        public bool CheckPermission(string pCode)
        {
            return true;
        }

        public void Shutdown() { }
    }
}