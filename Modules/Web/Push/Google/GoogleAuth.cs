using Nyan.Core.Shared;
using Nyan.Modules.Web.Push.Primitives;

namespace Nyan.Modules.Web.Push.Google
{
    [Priority(Level = -2)]
    public class GoogleAuth : IAuthPrimitive
    {
        public GoogleAuth() { Code = "Google"; }
        public string SenderId { get; set; }
        public string ServerKey { get; set; }
        public string Code { get; }
    }
}