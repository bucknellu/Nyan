using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Core.Shared;

namespace Nyan.Modules.Storage.FileSystem
{
    [Priority(Level = -2)]
    public class FileSystemStorageProvider : IStorageProvider
    {
        public FileSystemStorageProvider()
        {
            Configuration = new FileSystemStorageConfiguration();
            try { Directory.CreateDirectory(Configuration.StoragePath[0]); } catch
            {
                // ignored
            }
        }

        public FileSystemStorageConfiguration Configuration { get; set; }

        public string Suffix { get; set; } = "";

        public List<string> LegacySuffixes { get; set; } = new List<string> {".storage"};

        public Stream this[string key] { get => Get(key); set => Put(value, key); }

        public EOperationalStatus OperationalStatus { get; } = EOperationalStatus.Operational;

        private List<string> _preCompiledPaths = null;

        public FileStream Get(string key)
        {
            var path = GetFullPath(key);
            Current.Log.Add($"Stream {key} GET");

            foreach (var i in path)
            {
                if (!File.Exists(i)) continue;
                return new FileStream(i, FileMode.Open);
            }

            Current.Log.Add($"Stream {key} NOT FOUND", Message.EContentType.Warning);

            return null;
        }

        public List<string> GetFullPath(string key)
        {
            if (_preCompiledPaths != null) return _preCompiledPaths.Select(i => string.Format(i, key)).ToList();

            var ret = Configuration.StoragePath.Select(i => i + "{0}" + Suffix).ToList();
            if (LegacySuffixes.Count > 0) ret.AddRange(LegacySuffixes.SelectMany(i => Configuration.StoragePath.Select(j => j + "{0}" + i)));

            _preCompiledPaths = ret;

            return GetFullPath(key);
        }

        public string GetBasePath() { return Configuration.StoragePath[0]; }

        public IEnumerable<string> GetKeys()
        {
            var suffixLength = Suffix.Length;

            var files = Directory.GetFiles(Configuration.StoragePath[0], "*" + Suffix, SearchOption.TopDirectoryOnly)
                .Select(i => i.Substring(i.Length - suffixLength));

            return files;
        }

        public bool Exists(string key)
        {
            var path = GetFullPath(key);
            foreach (var i in path)
            {
                if (!File.Exists(i)) continue;

                return true;
            }

            return false;
        }

        public void Remove(string key)
        {
            if (!Exists(key))
            {
                Current.Log.Add($"Stream DEL {key} NOT FOUND", Message.EContentType.Warning);
                return;
            }

            var path = GetFullPath(key);
            foreach (var i in path)
            {
                if (!File.Exists(i)) continue;

                Current.Log.Add($"Stream DEL {key} @ {i}");

                File.Delete(i);
                return;
            }
        }

        public void RemoveAll()
        {
            var di = new DirectoryInfo(Configuration.StoragePath[0]); // It'll only remove from the CURRENT repo. Historical repos are preserved.

            var suffixLength = Suffix.Length;

            foreach (var file in di.GetFiles())
                if (file.Name.Substring(file.Name.Length - suffixLength) == Suffix)
                    file.Delete();
        }

        public void Initialize() { }
        public void Shutdown() { }

        public string Put(Stream source, string fileKey = null, bool partialName = false)
        {
            // PUT only writes to the current repo. Historical repos are preserved.

            if (fileKey == null || partialName)
                using (var md5 = MD5.Create()) { fileKey = $"{md5.ComputeHash(source).ToHex()}-{source.Length:X}{(fileKey != null ? "-" + fileKey : "")}"; }

            if (source == null)
            {
                Remove(fileKey);
                return fileKey;
            }

            if (File.Exists(GetFullPath(fileKey)[0])) return fileKey;

            source.Position = 0;
            var ms = new MemoryStream();
            source.CopyTo(ms);
            var buffer = ms.ToArray();

            var target = GetFullPath(fileKey)[0];

            File.WriteAllBytes(target, buffer);

            Current.Log.Add($"Stream PUT {buffer.Length}b @ {target}");

            ms.Dispose();

            return fileKey;
        }
    }
}