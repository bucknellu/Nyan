using Newtonsoft.Json;
using Nyan.Core.Settings;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml;

namespace Nyan.Modules.Web.Tools
{
    [RoutePrefix("stack/tools/proxy")]
    public class ProxyController : ApiController
    {
        [Route("http")]
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
        [Route("convert/xml/json")]
        [HttpGet]
        public object convert([FromUri] string url)
        {
            try
            {
                var contents = getUrlContents(url);

                // To convert an XML node contained in string xml into a JSON string   
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(contents);
                string jsonText = JsonConvert.SerializeXmlNode(doc);

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonText, System.Text.Encoding.UTF8, "application/json")
                };

                return resp;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }



        public string getUrlContents(string url)
        {
            try
            {
                return new HttpClient().GetStringAsync(url).Result;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }

    }
}