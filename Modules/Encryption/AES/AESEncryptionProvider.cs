using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Nyan.Core.Modules.Encryption;

namespace Nyan.Modules.Encryption.AES
{
    // ReSharper disable once InconsistentNaming
    public class AESEncryptionProvider : IEncryptionProvider
    {
        private readonly object _lock = new object();

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private RijndaelManaged _aesAlg;

        private bool _isInitialized;
        private string _rjiv = "";
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private string _rjkey = "";

        #region Instanced methods

        public AESEncryptionProvider()
        {
            _rjkey = "BuCkNeLlUnIvErSiTy29374857@(#*$$";
            _rjiv = "LaNdIt-EnTsYsTeM";
        }

        public AESEncryptionProvider(string key)
        {
            _rjkey = key;

            if (_rjkey.Length < 32) _rjkey = _rjkey.PadRight(32);

            _rjiv = "LaNdIt-EnTsYsTeM";
        }

        public AESEncryptionProvider(string key, string iv)
        {
            _rjkey = key;
            _rjiv = iv;
        }

        // ReSharper disable once InconsistentNaming
        public string Decrypt(string pContent)
        {
            InitSettings();

            string plaintext;

            var _base = Convert.FromBase64String(pContent);

            using (var msDecrypt = new MemoryStream(_base))
            {
                var deCryptT = _aesAlg.CreateDecryptor(_aesAlg.Key, _aesAlg.IV);

                using (var csDecrypt = new CryptoStream(msDecrypt, deCryptT, CryptoStreamMode.Read))
                {
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
            return plaintext;
        }

        // ReSharper disable once InconsistentNaming
        public string Encrypt(string pContent)
        {
            InitSettings();

            using (var msEncrypt = new MemoryStream())
            {
                string ret;

                var enCryptT = _aesAlg.CreateEncryptor(_aesAlg.Key, _aesAlg.IV);

                using (var csEncrypt = new CryptoStream(msEncrypt, enCryptT, CryptoStreamMode.Write))
                {
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(pContent);
                        swEncrypt.Flush();
                        csEncrypt.FlushFinalBlock();
                    }

                    ret = Convert.ToBase64String(msEncrypt.ToArray());
                }

                return ret;
            }
        }

        private void InitSettings()
        {
            if (_isInitialized) return;

            lock (_lock)
            {
                _aesAlg = new RijndaelManaged
                {
                    Key = Encoding.ASCII.GetBytes(_rjkey),
                    IV = Encoding.ASCII.GetBytes(_rjiv)
                };

                _isInitialized = true;
            }
        }

        #endregion

        public void Configure(params string[] oParms)
        {
            if (oParms.Length >= 1)
                _rjkey = oParms[0];

            if (oParms.Length >= 2)
                _rjiv = oParms[1];
        }
    }
}