using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace Nyan.Modules.Web.Push.Google.Notification
{
    public class NotificationPrimitive
    {
        public Options options = new Options();

        // https://developer.mozilla.org/en-US/docs/Web/API/ServiceWorkerRegistration/showNotification


        public string title { get; set; }
        public class Options
        {
            public List<Action> actions = new List<Action>();
            public string body { get; set; }
            public string targetUrl { get; set; }
            public string dir { get; set; } = "auto";
            public string lang { get; set; }
            public bool renotify { get; set; } = false;
            public bool requireInteraction { get; set; } = false;
            public string tag { get; set; }
            public List<int> vibrate { get; set; } = new List<int>();
            public string data { get; set; }
            public string icon { get; set; }
            public string badge { get; set; }
            public class Action
            {
                public Action(string action, string title = null, string icon = null, string url = null)
                {
                    this.action = action;

                    if (title == null) this.title = action;
                    else this.title = title;

                    if (url != null) this.url = url;

                    this.icon = icon;
                }

                public string action { get; set; }
                public string title { get; set; }
                public string icon { get; set; }
                public string url { get; set; }
            }
        }
    }
}