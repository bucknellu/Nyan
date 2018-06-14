using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Nyan.Core.Media;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST
{
    public class SimpleHttpResponseMessage : HttpResponseMessage
    {
        public SimpleHttpResponseMessage(Exception e)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            Content = new StringContent("Error: " + e.Message);
            Current.Log.Add(e);
        }

        public SimpleHttpResponseMessage(HttpStatusCode code, string content)
        {
            StatusCode = code;
            Content = new StringContent(content);
        }
    }

    public static class Extensions
    {
        public static HttpResponseMessage ToResponseMessage(this Image image, double cacheHours = 24)
        {
            var imgConverter = new ImageConverter();
            var imageBytes = (byte[])imgConverter.ConvertTo(image, typeof(byte[]));

            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imageBytes) };

            result.Headers.CacheControl = new CacheControlHeaderValue();
            result.Content.Headers.Add("Expires", DateTime.Now.AddHours(cacheHours).ToUniversalTime().ToString("R"));
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(image.GetCodec().MimeType);

            return result;
        }

        public static HttpResponseMessage FromPathToResponseMessage(this string filePath, double cacheHours = 24)
        {
            var dataBytes = File.ReadAllBytes(filePath);
            var dataStream = new MemoryStream(dataBytes);

            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(dataStream) };

            result.Headers.CacheControl = new CacheControlHeaderValue();
            result.Content.Headers.Add("Expires", DateTime.Now.AddHours(cacheHours).ToUniversalTime().ToString("R"));
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(filePath));

            return result;
        }
    }
}