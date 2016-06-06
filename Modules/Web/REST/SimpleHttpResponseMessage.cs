using System.Net;
using System.Net.Http;

namespace Nyan.Modules.Web.REST
{
    public class SimpleHttpResponseMessage: HttpResponseMessage
    {

        public SimpleHttpResponseMessage(HttpStatusCode code, string content)
        {
            StatusCode = code;
            Content = new StringContent(content);
        }

    }
}
