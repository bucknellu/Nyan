using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Nyan.Core.Settings;
using Nyan.Core.Shared;

namespace Nyan.Modules.Web.REST
{
    // Need to log asp.net webapi 2 request and response body to a database
    // https://stackoverflow.com/a/23660832/1845714

    /// <inheritdoc />
    [Priority(Level = -99)]
    public class RequestDelegatingHandler : DelegatingHandler, IRequestDelegatingHandler
    {
        public virtual void OnRequest(HttpRequestMessage request, string requestBody, string responseBody) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // let other handlers process the request
            var result = await base.SendAsync(request, cancellationToken);

            string responseBody = null;
            if (result.Content != null) responseBody = await result.Content.ReadAsStringAsync();

            // log request body
            var requestBody = await request.Content.ReadAsStringAsync();

            try { new Thread(() => OnRequest(request, requestBody, responseBody)).Start(); } catch (Exception e) { Current.Log.Add(e); }

            return result;
        }
    }
}