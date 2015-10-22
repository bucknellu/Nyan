namespace Nyan.Core.Modules.Encryption
{
    public class NullEncryptionProvider : IEncryptionProvider
    {
        public void Configure(params string[] oParms)
        {
        }

        public string Decrypt(string pContent)
        {
            //Pass-through
            return pContent;
        }

        public string Encrypt(string pContent)
        {
            //Pass-through
            return pContent;
        }
    }
}