using Nyan.Core.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nyan.Core.Settings;

namespace Nyan.Modules.Storage.FileSystem
{
    [Priority(Level = -2)]
    public class FileSystemStorageProvider : IStorageProvider
    {
        public FileSystemStorageProvider()
        {
            Configuration = new FileSystemStorageConfiguration();
            try
            {
                Directory.CreateDirectory(Configuration.StoragePath);
            }
            catch
            {
                // ignored
            }
        }

        public FileSystemStorageConfiguration Configuration { get; set; }

        public string Suffix { get; set; } = ".storage";

        public Stream this[string key] { get { return Get(key); } set { Put(value, key); } }

        public EOperationalStatus OperationalStatus { get; } = EOperationalStatus.Operational;

        public FileStream Get(string key)
        {
            Current.Log.Add("Stream GET " + GetFullPath(key));
            return new FileStream(GetFullPath(key), FileMode.Open);
        }

        public string GetFullPath(string key) { return Configuration.StoragePath + key + Suffix; }
        public string GetBasePath() { return Configuration.StoragePath; }

        public IEnumerable<string> GetKeys()
        {
            var suffixLength = Suffix.Length;

            var files = Directory.GetFiles(Configuration.StoragePath, "*" + Suffix, SearchOption.TopDirectoryOnly)
                .Select(i => i.Substring(i.Length - suffixLength));

            return files;
        }

        public bool Exists(string key) { return File.Exists(GetFullPath(key)); }

        public void Remove(string key)
        {
            Current.Log.Add("Stream DEL " + GetFullPath(key));
            File.Delete(GetFullPath(key));
        }

        public void RemoveAll()
        {
            var di = new DirectoryInfo(Configuration.StoragePath);

            var suffixLength = Suffix.Length;

            foreach (var file in di.GetFiles())
                if (file.Name.Substring(file.Name.Length - suffixLength) == Suffix)
                    file.Delete();
        }

        public void Initialize() { }
        public void Shutdown() { }

        public string Put(Stream source, string fileKey = null)
        {
            if (fileKey == null) fileKey = Guid.NewGuid().ToString();

            if (source == null)
            {
                Remove(fileKey);
                return fileKey;
            }

            source.Position = 0;
            var ms = new MemoryStream();
            source.CopyTo(ms);
            var buffer = ms.ToArray();

            File.WriteAllBytes(GetFullPath(fileKey), buffer);

            Current.Log.Add("Stream PUT " + buffer.Length + "b @ " + GetFullPath(fileKey));

            ms.Dispose();

            return fileKey;
        }

    }
}