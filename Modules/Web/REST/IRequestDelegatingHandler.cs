using System.Net.Http;

namespace Nyan.Modules.Web.REST
{
    public interface IRequestDelegatingHandler
    {
        void OnRequest(HttpRequestMessage request, string requestBody, string responseBody);
    }
}