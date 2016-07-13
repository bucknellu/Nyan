using System;
using System.Net;
using System.Net.Http;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST
{
    public class SimpleHttpResponseMessage: HttpResponseMessage
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
}
