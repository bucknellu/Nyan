using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Nyan.Core.Assembly;

namespace Nyan.Modules.Web.Tools.Security
{
    public static class Network
    {
        private static readonly NetworkDescriptorPrimitive Descriptor = new NetworkDescriptorPrimitive();

        private static readonly Dictionary<string, IpType> ResultCache = new Dictionary<string, IpType>();

        static Network()
        {
            var descriptors = Management.GetClassesByInterface<NetworkDescriptorPrimitive>();
            if (descriptors.Any()) Descriptor = (NetworkDescriptorPrimitive) Activator.CreateInstance(descriptors[0]);
        }

        /// <summary>
        ///     Identifies the characteristics of a given IP address.
        /// </summary>
        /// <param name="pIp">The Address to be checked.</param>
        /// <returns>An <seealso cref="IpType">IpType </seealso> object indicating the characteristics of a given IP address</returns>
        public static IpType Check(string pIp)
        {
            if (ResultCache.ContainsKey(pIp)) return ResultCache[pIp];

            Core.Settings.Current.Log.Add("Resolving IP context for " + pIp);

            try
            {
                foreach (var d in Descriptor.Descriptors)
                    if (d.IpNetworks.Any(n => IpNetwork.Contains(n, IPAddress.Parse(pIp))))
                    {
                        ResultCache[pIp] = d.Type;
                        return d.Type;
                    }

                var ret = new IpType {IsExternal = true, IsInternal = false, IsLocal = false, IsVpn = false};
                ResultCache[pIp] = ret;
                return ret;
            }
            catch (Exception e)
            {
                Core.Settings.Current.Log.Add(e, "Failed to resolve address " + pIp);
                var ret = new IpType {Resolved = false};
                ResultCache[pIp] = ret;
                return ret;
            }
        }

        public static IpType Current()
        {
            if (HttpContext.Current == null) throw new ArgumentNullException("No available HTTP context.");
            var remAddr = HttpContext.Current.Request.UserHostAddress;
            if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null) remAddr = HttpContext.Current.Request.Headers["HTTP_X_FORWARDED_FOR"];

            return Check(remAddr);
        }

        public class NetworkDescriptorPrimitive
        {
            public List<BlockDescriptor> Descriptors { get; set; } = new List<BlockDescriptor>();
        }

        public class BlockDescriptor
        {
            private List<string> _ipMask;
            internal List<IpNetwork> IpNetworks = new List<IpNetwork>();

            public IpType Type = new IpType();

            public List<string> IpMask
            {
                get { return _ipMask; }
                set
                {
                    _ipMask = value;

                    IpNetworks.Clear();
                    foreach (var i in _ipMask) IpNetworks.Add(IpNetwork.Parse(i));
                }
            }
        }

        public class IpNetworkCollection : IEnumerable<IpNetwork>, IEnumerator<IpNetwork>
        {
            private readonly byte _cidrSubnet;
            private readonly IpNetwork _ipnetwork;
            private double _enumerator;

            internal IpNetworkCollection(IpNetwork ipnetwork, byte cidrSubnet)
            {
                if (cidrSubnet > 32) throw new ArgumentOutOfRangeException(nameof(cidrSubnet));

                if (cidrSubnet < ipnetwork.Cidr) throw new ArgumentException("cidr");

                _cidrSubnet = cidrSubnet;
                _ipnetwork = ipnetwork;
                _enumerator = -1;
            }

            private byte Cidr => _ipnetwork.Cidr;

            private uint Broadcast => IpNetwork.ToUint(_ipnetwork.Broadcast);

            private uint Network => IpNetwork.ToUint(_ipnetwork.Network);

            #region Count, Array, Enumerator

            public double Count
            {
                get
                {
                    var count = Math.Pow(2, _cidrSubnet - Cidr);
                    return count;
                }
            }

            public IpNetwork this[double i]
            {
                get
                {
                    if (i >= Count) throw new ArgumentOutOfRangeException(nameof(i));

                    var size = Count;
                    var increment = (int) ((Broadcast - Network)/size);
                    var uintNetwork = (uint) (Network + (increment + 1)*i);
                    var ipn = new IpNetwork(uintNetwork, _cidrSubnet);
                    return ipn;
                }
            }

            #endregion

            #region IEnumerable Members

            IEnumerator<IpNetwork> IEnumerable<IpNetwork>.GetEnumerator() { return this; }

            IEnumerator IEnumerable.GetEnumerator() { return this; }

            #region IEnumerator<IPNetwork> Members

            public IpNetwork Current => this[_enumerator];

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                // nothing to dispose
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _enumerator++;
                return !(_enumerator >= Count);
            }

            public void Reset() { _enumerator = -1; }

            #endregion

            #endregion
        }
        public class IpNetwork : IComparable<IpNetwork>
        {
            #region properties

            //private uint _network;
            //private uint _netmask;
            //private uint _broadcast;
            //private uint _firstUsable;
            //private uint _lastUsable;
            //private uint _usable;
            private readonly uint _ipaddress;

            #endregion

            #region constructor

            internal IpNetwork(uint ipaddress, byte cidr)
            {
                if (cidr > 32) throw new ArgumentOutOfRangeException(nameof(cidr));

                _ipaddress = ipaddress;
                Cidr = cidr;
            }

            #endregion

            #region IComparable<IPNetwork> Members

            public int CompareTo(IpNetwork other)
            {
                var network = _network.CompareTo(other._network);
                if (network != 0) return network;

                var cidr = Cidr.CompareTo(other.Cidr);
                return cidr;
            }

            #endregion

            #region overlap

            /// <summary>
            ///     return true is network2 overlap network
            /// </summary>
            /// <param name="network"></param>
            /// <param name="network2"></param>
            /// <returns></returns>
            public static bool Overlap(IpNetwork network, IpNetwork network2)
            {
                if (network == null) throw new ArgumentNullException(nameof(network));

                if (network2 == null) throw new ArgumentNullException(nameof(network2));

                var uintNetwork = network._network;
                var uintBroadcast = network._broadcast;

                var uintFirst = network2._network;
                var uintLast = network2._broadcast;

                var overlap =
                    ((uintFirst >= uintNetwork) && (uintFirst <= uintBroadcast))
                    || ((uintLast >= uintNetwork) && (uintLast <= uintBroadcast))
                    || ((uintFirst <= uintNetwork) && (uintLast >= uintBroadcast))
                    || ((uintFirst >= uintNetwork) && (uintLast <= uintBroadcast));

                return overlap;
            }

            #endregion

            #region ToString

            public override string ToString() { return string.Format("{0}/{1}", Network, Cidr); }

            #endregion

            #region Equals

            public override bool Equals(object obj)
            {
                if (obj == null) return false;

                if (!(obj is IpNetwork)) return false;

                var remote = (IpNetwork) obj;
                if (_network != remote._network) return false;

                return Cidr == remote.Cidr;
            }

            #endregion

            #region GetHashCode

            public override int GetHashCode()
            {
                return string.Format("{0}|{1}|{2}",
                    _ipaddress.GetHashCode(),
                    _network.GetHashCode(),
                    Cidr.GetHashCode()).GetHashCode();
            }

            #endregion

            #region Print

            /// <summary>
            ///     Print an ipnetwork in a clear representation string
            /// </summary>
            /// <param name="ipnetwork"></param>
            /// <returns></returns>
            public static string Print(IpNetwork ipnetwork)
            {
                if (ipnetwork == null) throw new ArgumentNullException(nameof(ipnetwork));
                var sw = new StringWriter();

                sw.WriteLine("IPNetwork   : {0}", ipnetwork);
                sw.WriteLine("Network     : {0}", ipnetwork.Network);
                sw.WriteLine("Netmask     : {0}", ipnetwork.Netmask);
                sw.WriteLine("Cidr        : {0}", ipnetwork.Cidr);
                sw.WriteLine("Broadcast   : {0}", ipnetwork.Broadcast);
                sw.WriteLine("FirstUsable : {0}", ipnetwork.FirstUsable);
                sw.WriteLine("LastUsable  : {0}", ipnetwork.LastUsable);
                sw.WriteLine("Usable      : {0}", ipnetwork.Usable);

                return sw.ToString();
            }

            #endregion

            #region ListIPAddress

            public static IpAddressCollection ListIPAddress(IpNetwork ipnetwork)
            {
                return new IpAddressCollection(ipnetwork);
            }

            #endregion

            #region accessors

            private uint _network
            {
                get
                {
                    var uintNetwork = _ipaddress & _netmask;
                    return uintNetwork;
                }
            }

            /// <summary>
            ///     Network address
            /// </summary>
            public IPAddress Network { get { return ToIPAddress(_network); } }

            private uint _netmask { get { return ToUint(Cidr); } }

            /// <summary>
            ///     Netmask
            /// </summary>
            public IPAddress Netmask { get { return ToIPAddress(_netmask); } }

            private uint _broadcast
            {
                get
                {
                    var uintBroadcast = _network + ~_netmask;
                    return uintBroadcast;
                }
            }

            /// <summary>
            ///     Broadcast address
            /// </summary>
            public IPAddress Broadcast { get { return ToIPAddress(_broadcast); } }

            /// <summary>
            ///     First usable IP adress in Network
            /// </summary>
            public IPAddress FirstUsable
            {
                get
                {
                    var uintFirstUsable = Usable <= 0 ? _network : _network + 1;
                    return ToIPAddress(uintFirstUsable);
                }
            }

            /// <summary>
            ///     Last usable IP adress in Network
            /// </summary>
            public IPAddress LastUsable
            {
                get
                {
                    var uintLastUsable = Usable <= 0 ? _network : _broadcast - 1;
                    return ToIPAddress(uintLastUsable);
                }
            }

            /// <summary>
            ///     Number of usable IP adress in Network
            /// </summary>
            public uint Usable
            {
                get
                {
                    int cidr = ToCidr(_netmask);
                    var usableIps = cidr > 30 ? 0 : (0xffffffff >> cidr) - 1;
                    return usableIps;
                }
            }

            /// <summary>
            ///     The CIDR netmask notation
            /// </summary>
            public byte Cidr { get; }

            #endregion

            #region parsers

            /// <summary>
            ///     192.168.168.100 - 255.255.255.0
            ///     Network   : 192.168.168.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.168.1
            ///     End       : 192.168.168.254
            ///     Broadcast : 192.168.168.255
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <param name="netmask"></param>
            /// <returns></returns>
            public static IpNetwork Parse(string ipaddress, string netmask)
            {
                IpNetwork ipnetwork = null;
                InternalParse(false, ipaddress, netmask, out ipnetwork);
                return ipnetwork;
            }

            /// <summary>
            ///     192.168.168.100/24
            ///     Network   : 192.168.168.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.168.1
            ///     End       : 192.168.168.254
            ///     Broadcast : 192.168.168.255
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static IpNetwork Parse(string ipaddress, byte cidr)
            {
                IpNetwork ipnetwork = null;
                InternalParse(false, ipaddress, cidr, out ipnetwork);
                return ipnetwork;
            }

            /// <summary>
            ///     192.168.168.100 255.255.255.0
            ///     Network   : 192.168.168.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.168.1
            ///     End       : 192.168.168.254
            ///     Broadcast : 192.168.168.255
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <param name="netmask"></param>
            /// <returns></returns>
            public static IpNetwork Parse(IPAddress ipaddress, IPAddress netmask)
            {
                IpNetwork ipnetwork = null;
                InternalParse(false, ipaddress, netmask, out ipnetwork);
                return ipnetwork;
            }

            /// <summary>
            ///     192.168.0.1/24
            ///     192.168.0.1 255.255.255.0
            ///     Network   : 192.168.0.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.0.1
            ///     End       : 192.168.0.254
            ///     Broadcast : 192.168.0.255
            /// </summary>
            /// <param name="network"></param>
            /// <returns></returns>
            public static IpNetwork Parse(string network)
            {
                IpNetwork ipnetwork = null;
                InternalParse(false, network, out ipnetwork);
                return ipnetwork;
            }

            #endregion

            #region TryParse

            /// <summary>
            ///     192.168.168.100 - 255.255.255.0
            ///     Network   : 192.168.168.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.168.1
            ///     End       : 192.168.168.254
            ///     Broadcast : 192.168.168.255
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <param name="netmask"></param>
            /// <returns></returns>
            public static bool TryParse(string ipaddress, string netmask, out IpNetwork ipnetwork)
            {
                IpNetwork ipnetwork2 = null;
                InternalParse(true, ipaddress, netmask, out ipnetwork2);
                var parsed = ipnetwork2 != null;
                ipnetwork = ipnetwork2;
                return parsed;
            }


            /// <summary>
            ///     192.168.168.100/24
            ///     Network   : 192.168.168.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.168.1
            ///     End       : 192.168.168.254
            ///     Broadcast : 192.168.168.255
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static bool TryParse(string ipaddress, byte cidr, out IpNetwork ipnetwork)
            {
                IpNetwork ipnetwork2 = null;
                InternalParse(true, ipaddress, cidr, out ipnetwork2);
                var parsed = ipnetwork2 != null;
                ipnetwork = ipnetwork2;
                return parsed;
            }

            /// <summary>
            ///     192.168.0.1/24
            ///     192.168.0.1 255.255.255.0
            ///     Network   : 192.168.0.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.0.1
            ///     End       : 192.168.0.254
            ///     Broadcast : 192.168.0.255
            /// </summary>
            /// <param name="network"></param>
            /// <param name="ipnetwork"></param>
            /// <returns></returns>
            public static bool TryParse(string network, out IpNetwork ipnetwork)
            {
                IpNetwork ipnetwork2 = null;
                InternalParse(true, network, out ipnetwork2);
                var parsed = ipnetwork2 != null;
                ipnetwork = ipnetwork2;
                return parsed;
            }

            /// <summary>
            ///     192.168.0.1/24
            ///     192.168.0.1 255.255.255.0
            ///     Network   : 192.168.0.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.0.1
            ///     End       : 192.168.0.254
            ///     Broadcast : 192.168.0.255
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <param name="netmask"></param>
            /// <param name="ipnetwork"></param>
            /// <returns></returns>
            public static bool TryParse(IPAddress ipaddress, IPAddress netmask, out IpNetwork ipnetwork)
            {
                IpNetwork ipnetwork2 = null;
                InternalParse(true, ipaddress, netmask, out ipnetwork2);
                var parsed = ipnetwork2 != null;
                ipnetwork = ipnetwork2;
                return parsed;
            }

            #endregion

            #region InternalParse

            /// <summary>
            ///     192.168.168.100 - 255.255.255.0
            ///     Network   : 192.168.168.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.168.1
            ///     End       : 192.168.168.254
            ///     Broadcast : 192.168.168.255
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <param name="netmask"></param>
            /// <returns></returns>
            private static void InternalParse(bool tryParse, string ipaddress, string netmask, out IpNetwork ipnetwork)
            {
                if (string.IsNullOrEmpty(ipaddress))
                {
                    if (tryParse == false) throw new ArgumentNullException("ipaddress");
                    ipnetwork = null;
                    return;
                }

                if (string.IsNullOrEmpty(netmask))
                {
                    if (tryParse == false) throw new ArgumentNullException("netmask");
                    ipnetwork = null;
                    return;
                }

                IPAddress ip = null;
                var ipaddressParsed = IPAddress.TryParse(ipaddress, out ip);
                if (ipaddressParsed == false)
                {
                    if (tryParse == false) throw new ArgumentException("ipaddress");
                    ipnetwork = null;
                    return;
                }

                IPAddress mask = null;
                var netmaskParsed = IPAddress.TryParse(netmask, out mask);
                if (netmaskParsed == false)
                {
                    if (tryParse == false) throw new ArgumentException("netmask");
                    ipnetwork = null;
                    return;
                }

                InternalParse(tryParse, ip, mask, out ipnetwork);
            }

            private static void InternalParse(bool tryParse, string network, out IpNetwork ipnetwork)
            {
                if (string.IsNullOrEmpty(network))
                {
                    if (tryParse == false) throw new ArgumentNullException("network");
                    ipnetwork = null;
                    return;
                }

                network = Regex.Replace(network, @"[^0-9\.\/\s]+", "");
                network = Regex.Replace(network, @"\s{2,}", " ");
                network = network.Trim();
                var args = network.Split(' ', '/');
                byte cidr = 0;
                if (args.Length == 1)
                {
                    if (TryGuessCidr(args[0], out cidr))
                    {
                        InternalParse(tryParse, args[0], cidr, out ipnetwork);
                        return;
                    }

                    if (tryParse == false) throw new ArgumentException("network");
                    ipnetwork = null;
                    return;
                }

                if (byte.TryParse(args[1], out cidr))
                {
                    InternalParse(tryParse, args[0], cidr, out ipnetwork);
                    return;
                }

                InternalParse(tryParse, args[0], args[1], out ipnetwork);
            }


            /// <summary>
            ///     192.168.168.100 255.255.255.0
            ///     Network   : 192.168.168.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.168.1
            ///     End       : 192.168.168.254
            ///     Broadcast : 192.168.168.255
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <param name="netmask"></param>
            /// <returns></returns>
            private static void InternalParse(bool tryParse, IPAddress ipaddress, IPAddress netmask,
                out IpNetwork ipnetwork)
            {
                if (ipaddress == null)
                {
                    if (tryParse == false) throw new ArgumentNullException("ipaddress");
                    ipnetwork = null;
                    return;
                }

                if (netmask == null)
                {
                    if (tryParse == false) throw new ArgumentNullException("netmask");
                    ipnetwork = null;
                    return;
                }

                var uintIpAddress = ToUint(ipaddress);
                byte? cidr2 = null;
                var parsed = TryToCidr(netmask, out cidr2);
                if (parsed == false)
                {
                    if (tryParse == false) throw new ArgumentException("netmask");
                    ipnetwork = null;
                    return;
                }
                var cidr = (byte) cidr2;

                var ipnet = new IpNetwork(uintIpAddress, cidr);
                ipnetwork = ipnet;
            }


            /// <summary>
            ///     192.168.168.100/24
            ///     Network   : 192.168.168.0
            ///     Netmask   : 255.255.255.0
            ///     Cidr      : 24
            ///     Start     : 192.168.168.1
            ///     End       : 192.168.168.254
            ///     Broadcast : 192.168.168.255
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <param name="cidr"></param>
            /// <returns></returns>
            private static void InternalParse(bool tryParse, string ipaddress, byte cidr, out IpNetwork ipnetwork)
            {
                if (string.IsNullOrEmpty(ipaddress))
                {
                    if (tryParse == false) throw new ArgumentNullException("ipaddress");
                    ipnetwork = null;
                    return;
                }

                IPAddress ip = null;
                var ipaddressParsed = IPAddress.TryParse(ipaddress, out ip);
                if (ipaddressParsed == false)
                {
                    if (tryParse == false) throw new ArgumentException("ipaddress");
                    ipnetwork = null;
                    return;
                }

                IPAddress mask = null;
                var parsedNetmask = TryToNetmask(cidr, out mask);
                if (parsedNetmask == false)
                {
                    if (tryParse == false) throw new ArgumentException("cidr");
                    ipnetwork = null;
                    return;
                }

                InternalParse(tryParse, ip, mask, out ipnetwork);
            }

            #endregion

            #region converters

            #region ToUint

            /// <summary>
            ///     Convert an ipadress to decimal
            ///     0.0.0.0 -> 0
            ///     0.0.1.0 -> 256
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <returns></returns>
            public static uint ToUint(IPAddress ipaddress)
            {
                uint? uintIpAddress = null;
                InternalToUint(false, ipaddress, out uintIpAddress);
                return (uint) uintIpAddress;
            }

            /// <summary>
            ///     Convert an ipadress to decimal
            ///     0.0.0.0 -> 0
            ///     0.0.1.0 -> 256
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <returns></returns>
            public static bool TryToUint(IPAddress ipaddress, out uint? uintIpAddress)
            {
                uint? uintIpAddress2 = null;
                InternalToUint(true, ipaddress, out uintIpAddress2);
                var parsed = uintIpAddress2 != null;
                uintIpAddress = uintIpAddress2;
                return parsed;
            }

            private static void InternalToUint(bool tryParse, IPAddress ipaddress, out uint? uintIpAddress)
            {
                if (ipaddress == null)
                {
                    if (tryParse == false) throw new ArgumentNullException("ipaddress");
                    uintIpAddress = null;
                    return;
                }

                var bytes = ipaddress.GetAddressBytes();
                if (bytes.Length != 4)
                {
                    if (tryParse == false) throw new ArgumentException("bytes");
                    uintIpAddress = null;
                    return;
                }

                Array.Reverse(bytes);
                var value = BitConverter.ToUInt32(bytes, 0);
                uintIpAddress = value;
            }


            /// <summary>
            ///     Convert a cidr to uint netmask
            /// </summary>
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static uint ToUint(byte cidr)
            {
                uint? uintNetmask = null;
                InternalToUint(false, cidr, out uintNetmask);
                return (uint) uintNetmask;
            }


            /// <summary>
            ///     Convert a cidr to uint netmask
            /// </summary>
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static bool TryToUint(byte cidr, out uint? uintNetmask)
            {
                uint? uintNetmask2 = null;
                InternalToUint(true, cidr, out uintNetmask2);
                var parsed = uintNetmask2 != null;
                uintNetmask = uintNetmask2;
                return parsed;
            }

            /// <summary>
            ///     Convert a cidr to uint netmask
            /// </summary>
            /// <param name="cidr"></param>
            /// <returns></returns>
            private static void InternalToUint(bool tryParse, byte cidr, out uint? uintNetmask)
            {
                if (cidr > 32)
                {
                    if (tryParse == false) throw new ArgumentOutOfRangeException("cidr");
                    uintNetmask = null;
                    return;
                }
                var uintNetmask2 = cidr == 0 ? 0 : 0xffffffff << (32 - cidr);
                uintNetmask = uintNetmask2;
            }

            #endregion

            #region ToCidr

            /// <summary>
            ///     Convert netmask to CIDR
            ///     255.255.255.0 -> 24
            ///     255.255.0.0   -> 16
            ///     255.0.0.0     -> 8
            /// </summary>
            /// <param name="netmask"></param>
            /// <returns></returns>
            private static byte ToCidr(uint netmask)
            {
                byte? cidr = null;
                InternalToCidr(false, netmask, out cidr);
                return (byte) cidr;
            }

            /// <summary>
            ///     Convert netmask to CIDR
            ///     255.255.255.0 -> 24
            ///     255.255.0.0   -> 16
            ///     255.0.0.0     -> 8
            /// </summary>
            /// <param name="netmask"></param>
            /// <returns></returns>
            private static void InternalToCidr(bool tryParse, uint netmask, out byte? cidr)
            {
                if (!ValidNetmask(netmask))
                {
                    if (tryParse == false) throw new ArgumentException("netmask");
                    cidr = null;
                    return;
                }

                var cidr2 = BitsSet(netmask);
                cidr = cidr2;
            }

            /// <summary>
            ///     Convert netmask to CIDR
            ///     255.255.255.0 -> 24
            ///     255.255.0.0   -> 16
            ///     255.0.0.0     -> 8
            /// </summary>
            /// <param name="netmask"></param>
            /// <returns></returns>
            public static byte ToCidr(IPAddress netmask)
            {
                byte? cidr = null;
                InternalToCidr(false, netmask, out cidr);
                return (byte) cidr;
            }

            /// <summary>
            ///     Convert netmask to CIDR
            ///     255.255.255.0 -> 24
            ///     255.255.0.0   -> 16
            ///     255.0.0.0     -> 8
            /// </summary>
            /// <param name="netmask"></param>
            /// <returns></returns>
            public static bool TryToCidr(IPAddress netmask, out byte? cidr)
            {
                byte? cidr2 = null;
                InternalToCidr(true, netmask, out cidr2);
                var parsed = cidr2 != null;
                cidr = cidr2;
                return parsed;
            }

            private static void InternalToCidr(bool tryParse, IPAddress netmask, out byte? cidr)
            {
                if (netmask == null)
                {
                    if (tryParse == false) throw new ArgumentNullException("netmask");
                    cidr = null;
                    return;
                }
                uint? uintNetmask2 = null;
                var parsed = TryToUint(netmask, out uintNetmask2);
                if (parsed == false)
                {
                    if (tryParse == false) throw new ArgumentException("netmask");
                    cidr = null;
                    return;
                }
                var uintNetmask = (uint) uintNetmask2;

                byte? cidr2 = null;
                InternalToCidr(tryParse, uintNetmask, out cidr2);
                cidr = cidr2;
            }

            #endregion

            #region ToNetmask

            /// <summary>
            ///     Convert CIDR to netmask
            ///     24 -> 255.255.255.0
            ///     16 -> 255.255.0.0
            ///     8 -> 255.0.0.0
            /// </summary>
            /// <see cref="http://snipplr.com/view/15557/cidr-class-for-ipv4/" />
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static IPAddress ToNetmask(byte cidr)
            {
                IPAddress netmask = null;
                InternalToNetmask(false, cidr, out netmask);
                return netmask;
            }

            /// <summary>
            ///     Convert CIDR to netmask
            ///     24 -> 255.255.255.0
            ///     16 -> 255.255.0.0
            ///     8 -> 255.0.0.0
            /// </summary>
            /// <see cref="http://snipplr.com/view/15557/cidr-class-for-ipv4/" />
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static bool TryToNetmask(byte cidr, out IPAddress netmask)
            {
                IPAddress netmask2 = null;
                InternalToNetmask(true, cidr, out netmask2);
                var parsed = netmask2 != null;
                netmask = netmask2;
                return parsed;
            }


            private static void InternalToNetmask(bool tryParse, byte cidr, out IPAddress netmask)
            {
                if ((cidr < 0) || (cidr > 32))
                {
                    if (tryParse == false) throw new ArgumentOutOfRangeException("cidr");
                    netmask = null;
                    return;
                }
                var mask = ToUint(cidr);
                var netmask2 = ToIPAddress(mask);
                netmask = netmask2;
            }

            #endregion

            #endregion

            #region utils

            #region BitsSet

            /// <summary>
            ///     Count bits set to 1 in netmask
            /// </summary>
            /// <see
            ///     cref="http://stackoverflow.com/questions/109023/best-algorithm-to-count-the-number-of-set-bits-in-a-32-bit-integer" />
            /// <param name="netmask"></param>
            /// <returns></returns>
            private static byte BitsSet(uint netmask)
            {
                var i = netmask;
                i = i - ((i >> 1) & 0x55555555);
                i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
                i = (((i + (i >> 4)) & 0xf0f0f0f)*0x1010101) >> 24;
                return (byte) i;
            }

            /// <summary>
            ///     Count bits set to 1 in netmask
            /// </summary>
            /// <param name="netmask"></param>
            /// <returns></returns>
            public static byte BitsSet(IPAddress netmask)
            {
                var uintNetmask = ToUint(netmask);
                var bits = BitsSet(uintNetmask);
                return bits;
            }

            #endregion

            #region ValidNetmask

            /// <summary>
            ///     return true if netmask is a valid netmask
            ///     255.255.255.0, 255.0.0.0, 255.255.240.0, ...
            /// </summary>
            /// <see cref="http://www.actionsnip.com/snippets/tomo_atlacatl/calculate-if-a-netmask-is-valid--as2-" />
            /// <param name="netmask"></param>
            /// <returns></returns>
            public static bool ValidNetmask(IPAddress netmask)
            {
                if (netmask == null) throw new ArgumentNullException("netmask");
                var uintNetmask = ToUint(netmask);
                var valid = ValidNetmask(uintNetmask);
                return valid;
            }

            private static bool ValidNetmask(uint netmask)
            {
                var neg = ~(int) netmask & 0xffffffff;
                var isNetmask = ((neg + 1) & neg) == 0;
                return isNetmask;
            }

            #endregion

            #region ToIPAddress

            /// <summary>
            ///     Transform a uint ipaddress into IPAddress object
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <returns></returns>
            public static IPAddress ToIPAddress(uint ipaddress)
            {
                var bytes = BitConverter.GetBytes(ipaddress);
                Array.Reverse(bytes);
                var ip = new IPAddress(bytes);
                return ip;
            }

            #endregion

            #endregion

            #region contains

            /// <summary>
            ///     return true if ipaddress is contained in network
            /// </summary>
            /// <param name="network"></param>
            /// <param name="ipaddress"></param>
            /// <returns></returns>
            public static bool Contains(IpNetwork network, IPAddress ipaddress)
            {
                if (network == null) throw new ArgumentNullException("network");

                if (ipaddress == null) throw new ArgumentNullException("ipaddress");

                var uintNetwork = network._network;
                var uintBroadcast = network._broadcast;
                var uintAddress = ToUint(ipaddress);

                var contains = (uintAddress >= uintNetwork)
                               && (uintAddress <= uintBroadcast);

                return contains;
            }

            /// <summary>
            ///     return true is network2 is fully contained in network
            /// </summary>
            /// <param name="network"></param>
            /// <param name="network2"></param>
            /// <returns></returns>
            public static bool Contains(IpNetwork network, IpNetwork network2)
            {
                if (network == null) throw new ArgumentNullException("network");

                if (network2 == null) throw new ArgumentNullException("network2");

                var uintNetwork = network._network;
                var uintBroadcast = network._broadcast;

                var uintFirst = network2._network;
                var uintLast = network2._broadcast;

                var contains = (uintFirst >= uintNetwork)
                               && (uintLast <= uintBroadcast);

                return contains;
            }

            #endregion

            #region IANA block

            /// <summary>
            ///     10.0.0.0/8
            /// </summary>
            /// <returns></returns>
            public static IpNetwork IANA_ABLK_RESERVED1 { get; } = Parse("10.0.0.0/8");

            /// <summary>
            ///     172.12.0.0/12
            /// </summary>
            /// <returns></returns>
            public static IpNetwork IANA_BBLK_RESERVED1 { get; } = Parse("172.16.0.0/12");

            /// <summary>
            ///     192.168.0.0/16
            /// </summary>
            /// <returns></returns>
            public static IpNetwork IANA_CBLK_RESERVED1 { get; } = Parse("192.168.0.0/16");

            /// <summary>
            ///     return true if ipaddress is contained in
            ///     IANA_ABLK_RESERVED1, IANA_BBLK_RESERVED1, IANA_CBLK_RESERVED1
            /// </summary>
            /// <param name="ipaddress"></param>
            /// <returns></returns>
            public static bool IsIANAReserved(IPAddress ipaddress)
            {
                if (ipaddress == null) throw new ArgumentNullException("ipaddress");

                return Contains(IANA_ABLK_RESERVED1, ipaddress)
                       || Contains(IANA_BBLK_RESERVED1, ipaddress)
                       || Contains(IANA_CBLK_RESERVED1, ipaddress);
            }

            /// <summary>
            ///     return true if ipnetwork is contained in
            ///     IANA_ABLK_RESERVED1, IANA_BBLK_RESERVED1, IANA_CBLK_RESERVED1
            /// </summary>
            /// <param name="ipnetwork"></param>
            /// <returns></returns>
            public static bool IsIANAReserved(IpNetwork ipnetwork)
            {
                if (ipnetwork == null) throw new ArgumentNullException("ipnetwork");

                return Contains(IANA_ABLK_RESERVED1, ipnetwork)
                       || Contains(IANA_BBLK_RESERVED1, ipnetwork)
                       || Contains(IANA_CBLK_RESERVED1, ipnetwork);
            }

            #endregion

            #region Subnet

            /// <summary>
            ///     Subnet a network into multiple nets of cidr mask
            ///     Subnet 192.168.0.0/24 into cidr 25 gives 192.168.0.0/25, 192.168.0.128/25
            ///     Subnet 10.0.0.0/8 into cidr 9 gives 10.0.0.0/9, 10.128.0.0/9
            /// </summary>
            /// <param name="ipnetwork"></param>
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static IpNetworkCollection Subnet(IpNetwork network, byte cidr)
            {
                IpNetworkCollection ipnetworkCollection = null;
                InternalSubnet(false, network, cidr, out ipnetworkCollection);
                return ipnetworkCollection;
            }

            /// <summary>
            ///     Subnet a network into multiple nets of cidr mask
            ///     Subnet 192.168.0.0/24 into cidr 25 gives 192.168.0.0/25, 192.168.0.128/25
            ///     Subnet 10.0.0.0/8 into cidr 9 gives 10.0.0.0/9, 10.128.0.0/9
            /// </summary>
            /// <param name="ipnetwork"></param>
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static bool TrySubnet(IpNetwork network, byte cidr, out IpNetworkCollection ipnetworkCollection)
            {
                IpNetworkCollection inc = null;
                InternalSubnet(true, network, cidr, out inc);
                if (inc == null)
                {
                    ipnetworkCollection = null;
                    return false;
                }

                ipnetworkCollection = inc;
                return true;
            }

            private static void InternalSubnet(bool trySubnet, IpNetwork network, byte cidr,
                out IpNetworkCollection ipnetworkCollection)
            {
                if (network == null)
                {
                    if (trySubnet == false) throw new ArgumentNullException("network");
                    ipnetworkCollection = null;
                    return;
                }

                if (cidr > 32)
                {
                    if (trySubnet == false) throw new ArgumentOutOfRangeException("cidr");
                    ipnetworkCollection = null;
                    return;
                }

                if (cidr < network.Cidr)
                {
                    if (trySubnet == false) throw new ArgumentException("cidr");
                    ipnetworkCollection = null;
                    return;
                }

                ipnetworkCollection = new IpNetworkCollection(network, cidr);
            }

            #endregion

            #region Supernet

            /// <summary>
            ///     Supernet two consecutive cidr equal subnet into a single one
            ///     192.168.0.0/24 + 192.168.1.0/24 = 192.168.0.0/23
            ///     10.1.0.0/16 + 10.0.0.0/16 = 10.0.0.0/15
            ///     192.168.0.0/24 + 192.168.0.0/25 = 192.168.0.0/24
            /// </summary>
            /// <param name="network1"></param>
            /// <param name="network2"></param>
            /// <returns></returns>
            public static IpNetwork Supernet(IpNetwork network1, IpNetwork network2)
            {
                IpNetwork supernet = null;
                InternalSupernet(false, network1, network2, out supernet);
                return supernet;
            }

            /// <summary>
            ///     Try to supernet two consecutive cidr equal subnet into a single one
            ///     192.168.0.0/24 + 192.168.1.0/24 = 192.168.0.0/23
            ///     10.1.0.0/16 + 10.0.0.0/16 = 10.0.0.0/15
            ///     192.168.0.0/24 + 192.168.0.0/25 = 192.168.0.0/24
            /// </summary>
            /// <param name="network1"></param>
            /// <param name="network2"></param>
            /// <returns></returns>
            public static bool TrySupernet(IpNetwork network1, IpNetwork network2, out IpNetwork supernet)
            {
                IpNetwork outSupernet = null;
                InternalSupernet(true, network1, network2, out outSupernet);
                var parsed = outSupernet != null;
                supernet = outSupernet;
                return parsed;
            }

            private static void InternalSupernet(bool trySupernet, IpNetwork network1, IpNetwork network2,
                out IpNetwork supernet)
            {
                if (network1 == null)
                {
                    if (trySupernet == false) throw new ArgumentNullException("network1");
                    supernet = null;
                    return;
                }

                if (network2 == null)
                {
                    if (trySupernet == false) throw new ArgumentNullException("network2");
                    supernet = null;
                    return;
                }

                if (Contains(network1, network2))
                {
                    supernet = new IpNetwork(network1._network, network1.Cidr);
                    return;
                }

                if (Contains(network2, network1))
                {
                    supernet = new IpNetwork(network2._network, network2.Cidr);
                    return;
                }

                if (network1.Cidr != network2.Cidr)
                {
                    if (trySupernet == false) throw new ArgumentException("cidr");
                    supernet = null;
                    return;
                }

                var first = network1._network < network2._network ? network1 : network2;
                var last = network1._network > network2._network ? network1 : network2;

                /// Starting from here :
                /// network1 and network2 have the same cidr,
                /// network1 does not contain network2,
                /// network2 does not contain network1,
                /// first is the lower subnet
                /// last is the higher subnet

                if (first._broadcast + 1 != last._network)
                {
                    if (trySupernet == false) throw new ArgumentOutOfRangeException("network");
                    supernet = null;
                    return;
                }

                var uintSupernet = first._network;
                var cidrSupernet = (byte) (first.Cidr - 1);

                var networkSupernet = new IpNetwork(uintSupernet, cidrSupernet);
                if (networkSupernet._network != first._network)
                {
                    if (trySupernet == false) throw new ArgumentException("network");
                    supernet = null;
                    return;
                }
                supernet = networkSupernet;
            }

            #endregion

            #region SupernetArray

            /// <summary>
            ///     Supernet a list of subnet
            ///     192.168.0.0/24 + 192.168.1.0/24 = 192.168.0.0/23
            ///     192.168.0.0/24 + 192.168.1.0/24 + 192.168.2.0/24 + 192.168.3.0/24 = 192.168.0.0/22
            /// </summary>
            /// <param name="ipnetworks"></param>
            /// <param name="supernet"></param>
            /// <returns></returns>
            public static IpNetwork[] Supernet(IpNetwork[] ipnetworks)
            {
                IpNetwork[] supernet;
                InternalSupernet(false, ipnetworks, out supernet);
                return supernet;
            }

            /// <summary>
            ///     Supernet a list of subnet
            ///     192.168.0.0/24 + 192.168.1.0/24 = 192.168.0.0/23
            ///     192.168.0.0/24 + 192.168.1.0/24 + 192.168.2.0/24 + 192.168.3.0/24 = 192.168.0.0/22
            /// </summary>
            /// <param name="ipnetworks"></param>
            /// <param name="supernet"></param>
            /// <returns></returns>
            public static bool TrySupernet(IpNetwork[] ipnetworks, out IpNetwork[] supernet)
            {
                var supernetted = InternalSupernet(true, ipnetworks, out supernet);
                return supernetted;
            }

            public static bool InternalSupernet(bool trySupernet, IpNetwork[] ipnetworks, out IpNetwork[] supernet)
            {
                if (ipnetworks == null)
                {
                    if (trySupernet == false) throw new ArgumentNullException("ipnetworks");
                    supernet = null;
                    return false;
                }

                if (ipnetworks.Length <= 0)
                {
                    supernet = new IpNetwork[0];
                    return true;
                }

                var supernetted = new List<IpNetwork>();
                var ipns = Array2List(ipnetworks);
                var current = List2Stack(ipns);
                var previousCount = 0;
                var currentCount = current.Count;

                while (previousCount != currentCount)
                {
                    supernetted.Clear();
                    while (current.Count > 1)
                    {
                        var ipn1 = current.Pop();
                        var ipn2 = current.Peek();

                        IpNetwork outNetwork = null;
                        var success = TrySupernet(ipn1, ipn2, out outNetwork);
                        if (success)
                        {
                            current.Pop();
                            current.Push(outNetwork);
                        }
                        else
                        {
                            supernetted.Add(ipn1);
                        }
                    }
                    if (current.Count == 1) supernetted.Add(current.Pop());

                    previousCount = currentCount;
                    currentCount = supernetted.Count;
                    current = List2Stack(supernetted);
                }
                supernet = supernetted.ToArray();
                return true;
            }

            private static Stack<IpNetwork> List2Stack(List<IpNetwork> list)
            {
                var stack = new Stack<IpNetwork>();
                list.ForEach(delegate(IpNetwork ipn) { stack.Push(ipn); });
                return stack;
            }

            private static List<IpNetwork> Array2List(IpNetwork[] array)
            {
                var ipns = new List<IpNetwork>();
                ipns.AddRange(array);
                RemoveNull(ipns);
                ipns.Sort(delegate(IpNetwork ipn1, IpNetwork ipn2)
                {
                    var networkCompare = ipn1._network.CompareTo(ipn2._network);
                    if (networkCompare == 0)
                    {
                        var cidrCompare = ipn1.Cidr.CompareTo(ipn2.Cidr);
                        return cidrCompare;
                    }
                    return networkCompare;
                });
                ipns.Reverse();

                return ipns;
            }

            private static void RemoveNull(List<IpNetwork> ipns)
            {
                ipns.RemoveAll(delegate(IpNetwork ipn)
                {
                    if (ipn == null) return true;
                    return false;
                });
            }

            #endregion

            #region WideSubnet

            public static IpNetwork WideSubnet(string start, string end)
            {
                if (string.IsNullOrEmpty(start)) throw new ArgumentNullException("start");

                if (string.IsNullOrEmpty(end)) throw new ArgumentNullException("end");

                IPAddress startIP;
                if (!IPAddress.TryParse(start, out startIP)) throw new ArgumentException("start");

                IPAddress endIP;
                if (!IPAddress.TryParse(end, out endIP)) throw new ArgumentException("end");

                var ipnetwork = new IpNetwork(0, 0);
                for (byte cidr = 32; cidr >= 0; cidr--)
                {
                    var wideSubnet = Parse(start, cidr);
                    if (Contains(wideSubnet, endIP))
                    {
                        ipnetwork = wideSubnet;
                        break;
                    }
                }
                return ipnetwork;
            }

            public static bool TryWideSubnet(IpNetwork[] ipnetworks, out IpNetwork ipnetwork)
            {
                IpNetwork ipn = null;
                InternalWideSubnet(true, ipnetworks, out ipn);
                if (ipn == null)
                {
                    ipnetwork = null;
                    return false;
                }
                ipnetwork = ipn;
                return true;
            }

            public static IpNetwork WideSubnet(IpNetwork[] ipnetworks)
            {
                IpNetwork ipn = null;
                InternalWideSubnet(false, ipnetworks, out ipn);
                return ipn;
            }

            private static void InternalWideSubnet(bool tryWide, IpNetwork[] ipnetworks, out IpNetwork ipnetwork)
            {
                if (ipnetworks == null)
                {
                    if (tryWide == false) throw new ArgumentNullException("ipnetworks");
                    ipnetwork = null;
                    return;
                }

                var nnin = Array.FindAll(ipnetworks, delegate(IpNetwork ipnet) { return ipnet != null; });

                if (nnin.Length <= 0)
                {
                    if (tryWide == false) throw new ArgumentException("ipnetworks");
                    ipnetwork = null;
                    return;
                }

                if (nnin.Length == 1)
                {
                    var ipn0 = nnin[0];
                    ipnetwork = ipn0;
                    return;
                }

                Array.Sort(nnin);
                var nnin0 = nnin[0];
                var uintNnin0 = nnin0._ipaddress;

                var nninX = nnin[nnin.Length - 1];
                var ipaddressX = nninX.Broadcast;

                var ipn = new IpNetwork(0, 0);
                for (var cidr = nnin0.Cidr; cidr >= 0; cidr--)
                {
                    var wideSubnet = new IpNetwork(uintNnin0, cidr);
                    if (Contains(wideSubnet, ipaddressX))
                    {
                        ipn = wideSubnet;
                        break;
                    }
                }

                ipnetwork = ipn;
            }

            #endregion

            #region TryGuessCidr

            /// <summary>
            ///     Class              Leading bits    Default netmask
            ///     A (CIDR /8)	       00           255.0.0.0
            ///     A (CIDR /8)	       01           255.0.0.0
            ///     B (CIDR /16)	   10           255.255.0.0
            ///     C (CIDR /24)       11 	        255.255.255.0
            /// </summary>
            /// <param name="ip"></param>
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static bool TryGuessCidr(string ip, out byte cidr)
            {
                IPAddress ipaddress = null;
                var parsed = IPAddress.TryParse(string.Format("{0}", ip), out ipaddress);
                if (parsed == false)
                {
                    cidr = 0;
                    return false;
                }
                var uintIPAddress = ToUint(ipaddress);
                uintIPAddress = uintIPAddress >> 29;
                if (uintIPAddress <= 3)
                {
                    cidr = 8;
                    return true;
                }
                if (uintIPAddress <= 5)
                {
                    cidr = 16;
                    return true;
                }
                if (uintIPAddress <= 6)
                {
                    cidr = 24;
                    return true;
                }

                cidr = 0;
                return false;
            }

            /// <summary>
            ///     Try to parse cidr. Have to be >= 0 and <= 32
            /// </summary>
            /// <param name="sidr"></param>
            /// <param name="cidr"></param>
            /// <returns></returns>
            public static bool TryParseCidr(string sidr, out byte? cidr)
            {
                byte b = 0;
                if (!byte.TryParse(sidr, out b))
                {
                    cidr = null;
                    return false;
                }

                IPAddress netmask = null;
                if (!TryToNetmask(b, out netmask))
                {
                    cidr = null;
                    return false;
                }

                cidr = b;
                return true;
            }

            #endregion
        }
        public class IpAddressCollection : IEnumerable<IPAddress>, IEnumerator<IPAddress>
        {
            private readonly IpNetwork _ipnetwork;
            private double _enumerator;

            internal IpAddressCollection(IpNetwork ipnetwork)
            {
                _ipnetwork = ipnetwork;
                _enumerator = -1;
            }

            #region Count, Array, Enumerator

            public double Count { get { return _ipnetwork.Usable + 2; } }

            public IPAddress this[double i]
            {
                get
                {
                    if (i >= Count) throw new ArgumentOutOfRangeException("i");

                    var ipn = IpNetwork.Subnet(_ipnetwork, 32);
                    return ipn[i].Network;
                }
            }

            #endregion

            #region IEnumerable Members

            IEnumerator<IPAddress> IEnumerable<IPAddress>.GetEnumerator() { return this; }

            IEnumerator IEnumerable.GetEnumerator() { return this; }

            #region IEnumerator<IPNetwork> Members

            public IPAddress Current { get { return this[_enumerator]; } }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                // nothing to dispose
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current { get { return Current; } }

            public bool MoveNext()
            {
                _enumerator++;
                if (_enumerator >= Count) return false;
                return true;
            }

            public void Reset() { _enumerator = -1; }

            #endregion

            #endregion
        }

        /// <summary>
        ///     Response type indicating the characteristics of a given IP address.
        /// </summary>
        public class IpType
        {
            public bool IsExternal;
            public bool IsInternal;
            public bool IsLocal;
            public bool IsVpn;
            public bool Resolved = true;
        }
    }
}