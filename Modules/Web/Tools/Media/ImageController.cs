using Nyan.Core.Extensions;
using Nyan.Core.Settings;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;

namespace Nyan.Modules.Web.Tools.Media
{
    [RoutePrefix("stack/tools/image")]
    public class ImageController : ApiController
    {
        [Route("external")]
        [HttpGet]
        public virtual HttpResponseMessage GetReference([FromUri] string url, int? width = null, int? height = null)
        {
            Uri uriResult;

            if (url.IndexOf("http", StringComparison.Ordinal) == -1)
            {
                url = string.Format("http://{0}", url);
            }

            //can contain the text http and contain incorrect uri scheme.
            var isUrl = Uri.TryCreate(url, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!isUrl)
                throw new ArgumentException("Parameter is invalid: url");

            var cacheDir = Current.DataDirectory + "\\cache";

            Image image;

            var imgConverter = new ImageConverter();

            var dimensionsPart = "";

            if (width != null)
                dimensionsPart += "w" + width;

            if (height != null)
                dimensionsPart += "h" + height;

            var outtype = MimeMapping.GetMimeMapping(url);

            if (outtype == "application/octet-stream") outtype = "image/png";

            var md5Url = url.Md5Hash();
            var preCompName = cacheDir + "\\media-external-" + md5Url + "-" + dimensionsPart + ".png";

            Current.Log.Add(outtype + " : " + preCompName);

            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            if (File.Exists(preCompName))
            {
                image = new Bitmap(preCompName);
                outtype = "image/png";
            }
            else
            {
                //connect via web client and get the photo at the specified url


                Current.Log.Add("Fetching external resource: " + url);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

                using (var httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    using (var stream = httpWebReponse.GetResponseStream())
                    {
                        image = Image.FromStream(stream);

                        if (image == null)
                            throw new FileNotFoundException(url + " could not be found");

                        if (width == null)
                            width = image.Width;

                        if (height == null)
                            height = image.Height;

                        if ((int)height * (int)width > 2560000)
                            throw new ArgumentException(
                                "Combined size is invalid. Limit pixel count to 2560000 (width x height).");

                        //resize image
                        if ((width != image.Width) || (height != image.Height))
                        {
                            image = Utilities.ResizeImage(image, (int)width, (int)height);
                            Current.Log.Add("Caching compiled image: " + preCompName);
                            image.Save(preCompName, ImageFormat.Png);
                        }
                    }
                }
            }

            var imageBytes = (byte[])(imgConverter.ConvertTo(image, typeof(byte[])));

            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imageBytes) };

            result.Headers.CacheControl = new CacheControlHeaderValue();
            result.Content.Headers.Add("Expires", DateTime.Now.AddDays(3).ToUniversalTime().ToString(("R")));
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(outtype);

            return result;
        }
    }
}