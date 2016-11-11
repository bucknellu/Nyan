using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Nyan.Core.Extensions;
using Nyan.Core.Settings;
using Nyan.Modules.Web.Push.Model;
using Nyan.Modules.Web.Push.Properties;

// ReSharper disable InconsistentNaming

namespace Nyan.Modules.Web.Push.Controllers
{
    [RoutePrefix("stack/communication/push")]
    public class PushController : ApiController
    {
        [Route("resources")]
        [HttpGet]
        public object GetResources()
        {
            var list = GetType().Assembly.GetManifestResourceNames();
            return list;
        }

        [Route("resources/pushServiceWorker")]
        [HttpGet]
        public virtual HttpResponseMessage GetPushServiceWorker()
        {
            var result = Request.CreateResponse(HttpStatusCode.OK);
            result.Content = new StringContent(Resources.pushServiceWorker);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/javascript");
            result.Headers.Add("Service-Worker-Allowed", "/");
            return result;
        }

        [Route("register")]
        [HttpPost]
        public virtual object DoRegister([FromBody] EndpointEntry ep)
        {
            try
            {
                Instances.Dispatcher.Register(ep);
                return true;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                return false;
            }

        }

        [Route("deregister")]
        [HttpPost]
        public virtual object DoDeregister([FromBody] EndpointEntry ep)
        {
            try
            {
                Instances.Dispatcher.Deregister(ep);
                Current.Log.Add(ep.ToJson());
                return true;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                return false;
            }
        }

    }
}