using System.IO;
using System.Text;

namespace Nyan.Modules.Web.REST
{
    public class StreamWatcher : Stream
    {
        private readonly Stream _base;
        private readonly MemoryStream _memoryStream = new MemoryStream();
        public StreamWatcher(Stream stream) { _base = stream; }
        public override void Flush() { _base.Flush(); }
        public override int Read(byte[] buffer, int offset, int count) { return _base.Read(buffer, offset, count); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _memoryStream.Write(buffer, offset, count);
            _base.Write(buffer, offset, count);
        }

        public override string ToString() { return Encoding.UTF8.GetString(_memoryStream.ToArray()); }

        #region Rest of the overrides

        public override bool CanRead => _base.CanRead;
        public override bool CanSeek => _base.CanSeek;
        public override bool CanWrite => _base.CanWrite;
        public override long Length => _base.Length;
        public override long Position { get => _base.Position; set => _base.Position = value; }
        public override long Seek(long offset, SeekOrigin origin) { return _base.Seek(offset, origin); }
        public override void SetLength(long value) { _base.SetLength(value); }

        #endregion
    }
}