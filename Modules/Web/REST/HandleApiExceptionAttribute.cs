using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Web.REST
{
    public class HandleApiExceptionAttribute : ExceptionFilterAttribute
    {
        //http://stackoverflow.com/questions/16243021/return-custom-error-objects-in-web-api
        public override void OnException(HttpActionExecutedContext context)
        {
            var request = context.ActionContext.Request;

            object response = null;

            response = new {context.Exception.Message, Stack = context.Exception.FancyString()};

            context.Response = request.CreateResponse(HttpStatusCode.BadRequest, response);
        }
    }
}