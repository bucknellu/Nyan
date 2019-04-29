using System.Net.Http;
using System.Web.Http;
using Nyan.Core.Modules.Diagnostics;

namespace Nyan.Modules.Web.REST.Diagnostics
{
    [RoutePrefix("stack/diagnostics")]
    public class StackToolsDiagnosticsController : ApiController
    {
        #region HTTP Methods

        [Route("heartbeat/{category}"), HttpGet]
        public virtual HttpResponseMessage StackToolsDiagnosticsRunByCategory(string category) { return Factory.RunDiagnostics(category).AsResponse(); }

        [Route("heartbeat"), HttpGet]
        public virtual HttpResponseMessage StackToolsDiagnosticsRun() { return Factory.RunDiagnostics().AsResponse(); }

        #endregion
    }
}