using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST
{
    public static class ControllerHelper
    {
        public static void ProcessPipelineHeaders<T>(HttpHeaders retHeaders) where T : MicroEntity<T>
        {
            foreach (var i in MicroEntity<T>.Statements.BeforeActionPipeline)
            {
                var hs = i.Headers<T>();
                if (hs == null) continue;
                foreach (var j in hs) retHeaders.Add(j.Key, j.Value);
            }

            foreach (var i in MicroEntity<T>.Statements.AfterActionPipeline)
            {
                var hs = i.Headers<T>();
                if (hs == null) continue;
                foreach (var j in hs) retHeaders.Add(j.Key, j.Value);
            }
        }

        public static HttpResponseMessage RenderJsonResult(object contents)
        {
            try
            {
                var ret = new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent(contents.ToJson())};
                ret.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return ret;
            }
            catch (Exception e)
            {
                Current.Log.Add(e, "RenderJsonResult ERR: ");
                Current.Log.Add("RenderJsonResult" + contents.ToJson(), Message.EContentType.Info);
                throw;
            }
        }

        public static void RenderException(HttpStatusCode eType, string message, HttpRequestMessage request)
        {
            var httpError = new HttpError(message);
            var errorResponse = request.CreateErrorResponse(eType, httpError);
            throw new HttpResponseException(errorResponse);
        }
    }
}