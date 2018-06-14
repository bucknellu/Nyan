using System.Net.Http;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Web.REST
{
    public static class Instances
    {
        private static DelegatingHandler _requestDelegatingHandlerInstance;

        private static readonly object Padlock = new object();

        public static DelegatingHandler DelegatingHandler
        {
            get
            {
                if (_requestDelegatingHandlerInstance != null) return _requestDelegatingHandlerInstance;

                lock (Padlock)
                {
                    if (_requestDelegatingHandlerInstance != null) return _requestDelegatingHandlerInstance;

                    //There'll be at least one, the default.
                    _requestDelegatingHandlerInstance = Management.GetClassesByInterface<IRequestDelegatingHandler>()[0].CreateInstance<DelegatingHandler>();
                    return _requestDelegatingHandlerInstance;
                }
            }
        }
    }
}