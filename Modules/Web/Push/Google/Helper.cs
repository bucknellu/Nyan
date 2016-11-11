using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Nyan.Core.Extensions;
using Nyan.Modules.Web.Push.Model;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

// ReSharper disable InconsistentNaming

namespace Nyan.Modules.Web.Push.Google
{
    // Source: http://stackoverflow.com/a/39839330/1845714
    public static class Helper
    {
        private static readonly string FirebaseServerKey = ((GoogleAuth)Instances.Auth).ServerKey;

        /// <summary>
        ///     Base 64 Encoding with URL and Filename Safe Alphabet using UTF-8 character set.
        /// </summary>
        /// <param name="str">The origianl string</param>
        /// <returns>The Base64 encoded string</returns>
        public static string Base64ForUrlEncode(string str)
        {
            var encbuff = Encoding.UTF8.GetBytes(str);
            return HttpServerUtility.UrlTokenEncode(encbuff);
        }

        /// <summary>
        ///     Decode Base64 encoded string with URL and Filename Safe Alphabet using UTF-8.
        /// </summary>
        /// <param name="str">Base64 code</param>
        /// <returns>The decoded string.</returns>
        public static string Base64ForUrlDecode(string str)
        {
            var decbuff = HttpServerUtility.UrlTokenDecode(str);
            return Encoding.UTF8.GetString(decbuff);
        }

        public static string Base64UrlEncode(byte[] arg)
        {
            var s = Convert.ToBase64String(arg); // Regular base64 encoder
            s = s.Split('=')[0]; // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding
            return s;
        }

        public static byte[] Base64UrlDecode(string arg)
        {
            var s = arg;
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding
            switch (s.Length % 4) // Pad with trailing '='s
            {
                case 0:
                    break; // No pad chars in this case
                case 2:
                    s += "==";
                    break; // Two pad chars
                case 3:
                    s += "=";
                    break; // One pad char
                default:
                    throw new Exception(
                        "Illegal base64url string!");
            }
            return Convert.FromBase64String(s); // Standard base64 decoder
        }

        public static bool SendNotification(EndpointEntry sub, byte[] data, int ttl = 0, ushort padding = 0,
            bool randomisePadding = false)
        {
            return SendNotification(sub.endpoint,
                data: data,
                userKey: Base64UrlDecode(sub.keys.p256dh),
                userSecret: Base64UrlDecode(sub.keys.auth),
                ttl: ttl,
                padding: padding,
                randomisePadding: randomisePadding);
        }

        public static bool SendNotification(string endpoint, string data, string userKey, string userSecret, int ttl = 0,
            ushort padding = 0, bool randomisePadding = false)
        {
            return SendNotification(endpoint,
                data: Encoding.UTF8.GetBytes(data),
                userKey: Base64UrlDecode(userKey),
                userSecret: Base64UrlDecode(userSecret),
                ttl: ttl,
                padding: padding,
                randomisePadding: randomisePadding);
        }

        public static bool SendNotification(string endpoint, byte[] userKey, byte[] userSecret, byte[] data = null,
            int ttl = 0, ushort padding = 0, bool randomisePadding = false)
        {
            var Request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (endpoint.StartsWith("https://android.googleapis.com/gcm/send/")) Request.Headers.TryAddWithoutValidation("Authorization", "key=" + FirebaseServerKey);
            Request.Headers.Add("TTL", ttl.ToString());
            if ((data != null) && (userKey != null) && (userSecret != null))
            {
                var Package = EncryptMessage(userKey, userSecret, data, padding, randomisePadding);
                Request.Content = new ByteArrayContent(Package.Payload);
                Request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                Request.Content.Headers.ContentLength = Package.Payload.Length;
                Request.Content.Headers.ContentEncoding.Add("aesgcm");
                Request.Headers.Add("Crypto-Key", "keyid=p256dh;dh=" + Base64UrlEncode(Package.PublicKey));
                Request.Headers.Add("Encryption", "keyid=p256dh;salt=" + Base64UrlEncode(Package.Salt));
            }
            using (var HC = new HttpClient())
            {
                return HC.SendAsync(Request).Result.StatusCode == HttpStatusCode.Created;
            }
        }

        public static EncryptionResult EncryptMessage(byte[] userKey, byte[] userSecret, byte[] data, ushort padding = 0, bool randomisePadding = false)
        {
            var Random = new SecureRandom();
            var Salt = new byte[16];
            Random.NextBytes(Salt);
            var Curve = ECNamedCurveTable.GetByName("prime256v1");
            var Spec = new ECDomainParameters(Curve.Curve, Curve.G, Curve.N, Curve.H, Curve.GetSeed());
            var Generator = new ECKeyPairGenerator();
            Generator.Init(new ECKeyGenerationParameters(Spec, new SecureRandom()));
            var KeyPair = Generator.GenerateKeyPair();
            var AgreementGenerator = new ECDHBasicAgreement();
            AgreementGenerator.Init(KeyPair.Private);
            var IKM =
                AgreementGenerator.CalculateAgreement(new ECPublicKeyParameters(Spec.Curve.DecodePoint(userKey), Spec));
            var PRK = GenerateHKDF(userSecret, IKM.ToByteArrayUnsigned(),
                Encoding.UTF8.GetBytes("Content-Encoding: auth\0"), 32);
            var PublicKey = ((ECPublicKeyParameters)KeyPair.Public).Q.GetEncoded(false);
            var CEK = GenerateHKDF(Salt, PRK, CreateInfoChunk("aesgcm", userKey, PublicKey), 16);
            var Nonce = GenerateHKDF(Salt, PRK, CreateInfoChunk("nonce", userKey, PublicKey), 12);
            if (randomisePadding && (padding > 0)) padding = Convert.ToUInt16(Math.Abs(Random.NextInt()) % (padding + 1));
            var Input = new byte[padding + 2 + data.Length];
            Buffer.BlockCopy(ConvertInt(padding), 0, Input, 0, 2);
            Buffer.BlockCopy(data, 0, Input, padding + 2, data.Length);
            var Cipher = CipherUtilities.GetCipher("AES/GCM/NoPadding");
            Cipher.Init(true, new AeadParameters(new KeyParameter(CEK), 128, Nonce));
            var Message = new byte[Cipher.GetOutputSize(Input.Length)];
            Cipher.DoFinal(Input, 0, Input.Length, Message, 0);
            return new EncryptionResult { Salt = Salt, Payload = Message, PublicKey = PublicKey };
        }

        public static byte[] ConvertInt(int number)
        {
            var Output = BitConverter.GetBytes(Convert.ToUInt16(number));
            if (BitConverter.IsLittleEndian) Array.Reverse(Output);
            return Output;
        }

        public static byte[] CreateInfoChunk(string type, byte[] recipientPublicKey, byte[] senderPublicKey)
        {
            var Output = new List<byte>();
            Output.AddRange(Encoding.UTF8.GetBytes($"Content-Encoding: {type}\0P-256\0"));
            Output.AddRange(ConvertInt(recipientPublicKey.Length));
            Output.AddRange(recipientPublicKey);
            Output.AddRange(ConvertInt(senderPublicKey.Length));
            Output.AddRange(senderPublicKey);
            return Output.ToArray();
        }

        public static byte[] GenerateHKDF(byte[] salt, byte[] ikm, byte[] info, int len)
        {
            var PRKGen = MacUtilities.GetMac("HmacSHA256");
            PRKGen.Init(new KeyParameter(MacUtilities.CalculateMac("HmacSHA256", new KeyParameter(salt), ikm)));
            PRKGen.BlockUpdate(info, 0, info.Length);
            PRKGen.Update(1);
            var Result = MacUtilities.DoFinal(PRKGen);
            if (Result.Length > len) Array.Resize(ref Result, len);
            return Result;
        }

        public static bool SendNotification(EndpointEntry target, object obj)
        {
            return SendNotification(target, Encoding.UTF8.GetBytes(obj.ToJson()));
        }

        public class EncryptionResult
        {
            public byte[] PublicKey { get; set; }
            public byte[] Payload { get; set; }
            public byte[] Salt { get; set; }
        }
    }
}