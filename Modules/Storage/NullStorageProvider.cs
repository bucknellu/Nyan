using System.Collections.Generic;
using System.IO;
using Nyan.Core.Shared;

namespace Nyan.Modules.Storage
{
    [Priority(Level = -99)]
    public class NullStorageProvider : IStorageProvider
    {
        public Stream this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public EOperationalStatus OperationalStatus { get; } = EOperationalStatus.NonOperational;
        public string Put(Stream source, string fileKey = null, bool partialName = false) { throw new System.NotImplementedException(); }
        public FileStream Get(string key) { throw new System.NotImplementedException(); }
        public List<string> GetFullPath(string key) { throw new System.NotImplementedException(); }
        public string GetBasePath() { throw new System.NotImplementedException(); }
        public IEnumerable<string> GetKeys() { throw new System.NotImplementedException(); }
        public bool Exists(string key) { throw new System.NotImplementedException(); }
        public void Remove(string key) { throw new System.NotImplementedException(); }
        public void RemoveAll() { throw new System.NotImplementedException(); }
        public void Initialize() { throw new System.NotImplementedException(); }
        public void Shutdown() { throw new System.NotImplementedException(); }
    }
}