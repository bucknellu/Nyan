// ReSharper disable InconsistentNaming

namespace Nyan.Modules.Web.Push.Model
{
    public class EndpointEntry
    {
        public class Keys
        {
            public string p256dh { get; set; }
            public string auth { get; set; }
        }

        public string endpoint { get; set; }
        public Keys keys { get; set; }
    }
}