using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using Nyan.Core.Extensions;
using Nyan.Core.Media;
using Nyan.Core.Settings;
using Nyan.Modules.Web.REST;

namespace Nyan.Modules.Web.Tools.Media
{
    [RoutePrefix("stack/tools/image")]
    public class ImageController : ApiController
    {
        public virtual HttpResponseMessage GetReference([FromUri] string url, [FromUri] int? width = null, [FromUri] int? height = null) { return GetReference(url, width, height, true); }

        [Route("external"), HttpGet]
        public virtual HttpResponseMessage GetReference([FromUri] string url,
                                                        [FromUri] int? width = null, [FromUri] int? height = null,
                                                        [FromUri] bool crop = true,
                                                        [FromUri] int? w = null, [FromUri] int? h = null,
                                                        [FromUri] string format = null, [FromUri] string f = null,
                                                        [FromUri] string position = null, [FromUri] string p = null)
        {

            var redirects = new[] { ".svg" };

            var uri = new Uri(url);

            if (Path.HasExtension(uri.AbsoluteUri))
            {
                if (redirects.Any(i => Path.GetExtension(uri.AbsoluteUri).ToLower() == i))
                {
                    var response = Request.CreateResponse(HttpStatusCode.Moved);
                    response.Headers.Location = uri;
                    return response;
                }
            }

            return InternalGetReference(url, w ?? width, h ?? height, crop, f ?? format, p ?? position);
        }


        public virtual HttpResponseMessage InternalGetReference(string url, int? width = null, int? height = null, bool crop = true, string format = null, string position = null)
        {
            return Utilities.GetFormattedImageResourcePath(url, width, height, crop, format, position).FromPathToResponseMessage();
        }

        [Route("external/hash"), HttpGet]
        public virtual HttpResponseMessage GetReferenceByHash([FromUri] string key)
        {
            return InternalGetReference(Current.Encryption.Decrypt(key.FromBase64()));
        }
    }
}