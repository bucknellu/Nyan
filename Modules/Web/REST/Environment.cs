using System.Web;

namespace Nyan.Modules.Web.REST
{
    public static class Environment
    {
        public static string BaseUrl
        {
            get
            {
                var request = HttpContext.Current.Request;
                var baseUrl = request.Url.Scheme + "://" +
                              request.Url.Authority +
                              request.ApplicationPath.TrimEnd('/') + "/";
                return baseUrl;
            }
        }
        public static string ShortBaseUrl
        {
            get
            {
                var request = HttpContext.Current.Request;
                var baseUrl = request.ApplicationPath.TrimEnd('/') + "/";
                return baseUrl;
            }
        }
    }
}