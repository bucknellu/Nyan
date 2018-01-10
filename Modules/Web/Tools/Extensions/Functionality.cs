using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Web;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.Tools.Extensions
{
    public static class Network
    {

        public static string GetClientIp(this HttpRequestMessage request)
        {
            if (request == null) return null;
            if (request.Properties.ContainsKey("MS_HttpContext")) return ((HttpContextWrapper) request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            if (!request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name)) return HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            var prop = (RemoteEndpointMessageProperty) request.Properties[RemoteEndpointMessageProperty.Name];
            return prop.Address;
        }

        [DllImport("Iphlpapi.dll")]
        private static extern int SendARP(int dest, int host, ref long mac, ref int length);

        [DllImport("Ws2_32.dll")]
        private static extern int inet_addr(string ip);

        public static string GetMacAddress(string strClientIp)
        {
            var macDest = "";

            try
            {
                var ldest = inet_addr(strClientIp);
                inet_addr("");
                var macinfo = new long();
                var len = 6;
                SendARP(ldest, 0, ref macinfo, ref len);
                var macSrc = macinfo.ToString("X");

                while (macSrc.Length < 12) macSrc = macSrc.Insert(0, "0");

                for (var i = 0; i < 11; i++)
                    if (0 == i % 2)
                        if (i == 10) macDest = macDest.Insert(0, macSrc.Substring(i, 2));
                        else macDest = ":" + macDest.Insert(0, macSrc.Substring(i, 2));
            } catch (Exception err) { Current.Log.Add(err); }

            if (macDest == "") macDest = null;

            return macDest;
        }

        public static List<string> GetTraceRoute(string hostNameOrAddress) { return GetTraceRoute(hostNameOrAddress, 1).Select(i => i.ToString()).ToList(); }

        private static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress, int ttl)
        {
            const string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

            var pinger = new Ping();
            var pingerOptions = new PingOptions(ttl, true);
            const int timeout = 10000;
            var buffer = Encoding.ASCII.GetBytes(data);
            var reply = default(PingReply);

            reply = pinger.Send(hostNameOrAddress, timeout, buffer, pingerOptions);

            var result = new List<IPAddress>();

            switch (reply.Status)
            {
                case IPStatus.Success:
                    result.Add(reply.Address);
                    break;
                case IPStatus.TtlExpired:
                case IPStatus.TimedOut:
                    //add the currently returned address if an address was found with this TTL
                    if (reply.Status == IPStatus.TtlExpired) result.Add(reply.Address);
                    //recurse to get the next address...
                    var tempResult = default(IEnumerable<IPAddress>);
                    tempResult = GetTraceRoute(hostNameOrAddress, ttl + 1);
                    result.AddRange(tempResult);
                    break;
            }

            return result;
        }

        public static string GetDnsName(string ip)
        {
            try
            {
                var entry = Dns.GetHostEntry(ip);
                return entry.HostName;
            } catch (Exception) { return null; }
        }
    }
}