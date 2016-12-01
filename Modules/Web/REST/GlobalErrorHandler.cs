using System.Web.Http.ExceptionHandling;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST
{
    public class GlobalErrorHandler : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            Current.Log.Add(context.ExceptionContext.Exception);
        }
    }
}