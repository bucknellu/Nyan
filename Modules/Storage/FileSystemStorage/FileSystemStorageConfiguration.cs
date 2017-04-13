using Nyan.Core;

namespace Nyan.Modules.Storage.FileSystem
{
    public class FileSystemStorageConfiguration : IStorageConfiguration
    {
        public virtual string StoragePath => Configuration.DataDirectory + @"\storage\";
    }
}