namespace Nyan.Core.Modules.Encryption
{
    public interface IEncryptionProvider
    {
        void Configure(params string[] oParms);
        string Decrypt(string pContent);
        string Encrypt(string pContent);
        void Shutdown();
    }
}