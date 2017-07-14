using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nyan.Modules.Web.Tools.Stream
{
    public static class Extensions
    {
        public static IEnumerable<object> ControlFlow(this IEnumerable<object> ol, int sliceSize = 64, int sliceIndex = 0)
        {

            if (HttpContext.Current.Request.QueryString["i"] != null) sliceIndex = int.Parse(HttpContext.Current.Request.QueryString["i"]);
            if (HttpContext.Current.Request.QueryString["s"] != null) sliceSize = int.Parse(HttpContext.Current.Request.QueryString["s"]);

            var skipCount = sliceIndex * sliceSize;

            var buffer = ol
                .Skip(skipCount)
                .Take(sliceSize);



            return buffer;
        }
    }
}