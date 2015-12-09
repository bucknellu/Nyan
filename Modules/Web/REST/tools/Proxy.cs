using System;
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

                return result;
            } catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }
    }
}