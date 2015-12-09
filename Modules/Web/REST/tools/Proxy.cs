using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST.tools
{
    [RoutePrefix("stack/proxy")]
    public class ProxyController : ApiController
    {
        [Route("")]
        [HttpGet]
        public object Search([FromUri] string url)
        {
            try
            {
                var client = new HttpClient();
                var result = client.GetStringAsync(url).Result;

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain")
                };

                return resp;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }
    }
}