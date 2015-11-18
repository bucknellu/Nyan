using System.Security.Cryptography;
using System.Text;

namespace Nyan.Core.Factories
{
    public static class Identifier
    {
        private const string A = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        private const int MaxSize = 8;

        public static string MiniGuid()
        {
            var chars = A.ToCharArray();
            var data = new byte[1];
            var crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            const int size = MaxSize;
            data = new byte[size];
            crypto.GetNonZeroBytes(data);
            var result = new StringBuilder(size);
            
            foreach (var b in data)
                result.Append(chars[b%(chars.Length - 1)]);

            return result.ToString();
        }
    }
}