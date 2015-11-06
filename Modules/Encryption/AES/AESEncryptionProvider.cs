using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Nyan.Core.Modules.Encryption;

namespace Nyan.Modules.Encryption.AES
{
    public class AesEncryptionProvider : IEncryptionProvider
    {
        private readonly object _lock = new object();

        private RijndaelManaged _aesAlg;

        private bool _isInitialized;
        private string _rjiv = "";
        private string _rjkey = "";

        #region Instanced methods

        public AesEncryptionProvider()
        {
            _rjkey = "NyAn1DaTa2SeRvIcE3StAcK*#^$&%^#@";
                // I know. Default key and vector for encryption, right? This is just a demo, though.
            _rjiv = "NyAn-nYaN-NyAn-!"; 
            //This class should be properly instanced via the constructor below:
        }

        public AesEncryptionProvider(string key, string iv)
        {
            if (key.Length != 32) throw new ConfigurationErrorsException("An AES key must be 32 characters long.");
            if (iv.Length != 16) throw new ConfigurationErrorsException("An AES vector must be 16 characters long.");

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