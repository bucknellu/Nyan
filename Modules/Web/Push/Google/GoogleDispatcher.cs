using System;
using Nyan.Core.Extensions;
using Nyan.Core.Settings;
using Nyan.Core.Shared;
using Nyan.Modules.Web.Push.Model;
using Nyan.Modules.Web.Push.Primitives;

// ReSharper disable InconsistentNaming

namespace Nyan.Modules.Web.Push.Google
{
    [Priority(Level = -2)]
    public class GoogleDispatcher : DispatcherPrimitive
    {
        public override void Send(EndpointEntry target, object obj)
        {
            try
            {
                Current.Log.Add("GoogleDispatcher: SEND > " + target.endpoint);
                var ret = Helper.SendNotification(target, obj);
                Current.Log.Add("GoogleDispatcher: " + ret);
            }
            catch (Exception e) {
                Current.Log.Add(e);
            }
        }

        public class Payload
        {
            public string to { get; set; }
            public object data { get; set; }
        }
    }
}