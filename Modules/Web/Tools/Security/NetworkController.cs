using System.Web.Http;

namespace Nyan.Modules.Web.Tools.Security
{
    [RoutePrefix("stack/tools/network")]
    public class NetworkController : ApiController
    {
        [Route("type")]
        [HttpGet]
        public Network.IpType GetTypType() { return Network.Current(); }
    }
}