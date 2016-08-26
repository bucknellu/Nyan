using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Xml;
using Newtonsoft.Json;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.Tools.Resource
{
    [RoutePrefix("stack/tools/proxy")]
    public class ProxyController : ApiController
    {
        [Route("http")]
        [HttpGet]
        public object GetContent([FromUri] string url)
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
        public object GetContentAsJson([FromUri] string url)
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


        [Route("summary")]
        [HttpGet]
        public Summary GetSummary([FromUri] string url)
        {
            var result = new Summary();

            var uri = new UriBuilder(url).Uri;

            url = uri.AbsoluteUri;

            var request = WebRequest.Create(url) as HttpWebRequest;

            // If the request wasn't an HTTP request (like a file), ignore it
            if (request == null) return null;

            // Use the user's credentials
            request.UseDefaultCredentials = true;

            result.Url = request.RequestUri.ToString();

            // Obtain a response from the server, if there was an error, return nothing
            HttpWebResponse response = null;
            try { response = request.GetResponse() as HttpWebResponse; }
            catch (WebException)
            {
                return null;
            }

            // Regular expression for an HTML title

            // If the correct HTML header exists for HTML text, continue
            if (new List<string>(response.Headers.AllKeys).Contains("Content-Type"))
            {
                if (response.Headers["Content-Type"].StartsWith("text/html"))
                {
                    // Download the page
                    var web = new WebClient { UseDefaultCredentials = true };
                    var page = web.DownloadString(url);

                    // Extract the title
                    var ex = new Regex(@"(?<=<title.*>)([\s\S]*)(?=</title>)", RegexOptions.IgnoreCase);

                    result.Title = ex.Match(page).Value.Trim();
                }
            }

            // Not a valid HTML page
            return result;
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