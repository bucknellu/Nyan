using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nyan.Modules.Web.REST;

// ReSharper disable InconsistentNaming

namespace Nyan.Modules.Web.Tools.Metadata
{
    [RoutePrefix("stack/tools")]
    public class MetadataController : ApiController
    {
        [Route("metadata"), HttpGet]
        public virtual HttpResponseMessage GetMetadata([FromUri] string person = null,
                                                       [FromUri] string application = null)
        {

            var payload = new Dictionary<string, object>();

            if (person != null) payload.Add("PERSON", person);
            if (application != null) payload.Add("APPLICATION", application);


            return new SimpleHttpResponseMessage(HttpStatusCode.OK, Manager.Get(payload).ToString());
        }

        [Route("metadata"), HttpPost]
        public virtual HttpResponseMessage SetMetadata([FromBody] MetaPayload content)
        {
            Manager.Instance.Set(content.path, content.value, content.scope);
            return new SimpleHttpResponseMessage(HttpStatusCode.OK, "Success");
        }

        public class MetaPayload
        {
            public string path { get; set; }
            public object value { get; set; }
            public string scope { get; set; }
        }
    }
}