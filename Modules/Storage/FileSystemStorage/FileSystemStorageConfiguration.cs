using System.Collections.Generic;
using Nyan.Core;

namespace Nyan.Modules.Storage.FileSystem
{
    public class FileSystemStorageConfiguration : IStorageConfiguration
    {
        public virtual List<string> StoragePath => new List<string> { Configuration.DataDirectory + @"\storage\" };
    }
}