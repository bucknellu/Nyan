using System;
using System.Collections.Generic;
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
        internal static void HandleHeaders(HttpHeaders retHeaders, Dictionary<string, object> hs)
        {
            if (hs == null) return;

            foreach (var j in hs)
            {
                if (retHeaders.Contains(j.Key)) retHeaders.Remove(j.Key);
                retHeaders.Add(j.Key, j.Value.ToJson());
            }
        }
        public static void ProcessPipelineHeaders<T>(HttpHeaders retHeaders) where T : MicroEntity<T>
        {

            foreach (var i in MicroEntity<T>.Statements.BeforeActionPipeline) HandleHeaders(retHeaders, i.Headers<T>());
            foreach (var i in MicroEntity<T>.Statements.AfterActionPipeline) HandleHeaders(retHeaders, i.Headers<T>());
        }

        public static HttpResponseMessage RenderJsonResult(object contents)
        {
            try
            {
                var ret = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(contents.ToJson()) };
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

        public static HttpResponseMessage RenderStringResult(string contents)
        {
            try
            {
                var ret = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(contents) };
                ret.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return ret;
            }
            catch (Exception e)
            {
                Current.Log.Add(e, "RenderStringResult ERR: ");
                Current.Log.Add("RenderStringResult" + contents.ToJson(), Message.EContentType.Info);
                throw;
            }
        }

        public static HttpResponseMessage RenderExceptionMessage(Exception e, HttpRequestMessage request)
        {
            var httpError = new HttpError(e, true);
            var errorResponse = request.CreateErrorResponse(HttpStatusCode.InternalServerError, httpError);
            return errorResponse;
        }

        public static void RenderException(HttpStatusCode eType, string message, HttpRequestMessage request)
        {
            var httpError = new HttpError(message);
            var errorResponse = request.CreateErrorResponse(eType, httpError);
            throw new HttpResponseException(errorResponse);
        }
    }
}