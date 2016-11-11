using Nyan.Modules.Web.Push.Primitives;
// ReSharper disable InconsistentNaming

namespace Nyan.Modules.Web.Push.Google
{
    public class GoogleContent : IContentPrimitive
    {
        public string to { get; set; }
        public string priority { get; set; }
    }
}