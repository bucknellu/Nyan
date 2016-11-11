using System.Net;
using Nyan.Core.Shared;
using Nyan.Modules.Web.Push.Model;

namespace Nyan.Modules.Web.Push.Primitives
{
    [Priority(Level = -99)]
    public class DispatcherPrimitive
    {
        public virtual void Send(EndpointEntry ep, object obj) { }
        public virtual void Register(EndpointEntry ep) { }
        public virtual void Deregister(EndpointEntry ep) { }
    }
}