using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Nyan.Core.Extensions;
using Nyan.Core.Settings;
using Nyan.Modules.Web.Push.Google;
using Nyan.Modules.Web.Push.Primitives;

// ReSharper disable InconsistentNaming

namespace Nyan.Modules.Web.Push
{
    public static class FCM
    {
        public static void Send(string deviceId, IAuthPrimitive auth, IContentPrimitive content)
        {
            try
            {
                var gAuth = (GoogleAuth)auth;

                var applicationID = gAuth.ServerKey;
                var senderId = gAuth.SenderId;
                var tRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
                tRequest.Method = "post";
                tRequest.ContentType = "application/json";

                var data = new
                {
                    to = deviceId,
                    notification = new
                    {
                        body = "This is the message Body",
                        title = "This is the title of Message",
                        icon = "myicon"
                    },
                    priority = "high"
                };

                var json = data.ToJson();
                var byteArray = Encoding.UTF8.GetBytes(json);
                tRequest.Headers.Add("Authorization:key={0}".format(applicationID));
                tRequest.Headers.Add("Sender: id={0}".format(senderId));
                tRequest.ContentLength = byteArray.Length;

                using (var dataStream = tRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);

                    using (var tResponse = tRequest.GetResponse())
                    {
                        using (var dataStreamResponse = tResponse.GetResponseStream())
                        {
                            using (var tReader = new StreamReader(dataStreamResponse))
                            {
                                var sResponseFromServer = tReader.ReadToEnd();
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Current.Log.Add(ex);
            }
        }
    }
}

