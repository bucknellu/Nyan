using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Nyan.Core.Media
{
    public static class Utilities
    {
        private static Dictionary<Guid, ImageCodecInfo> _imageFormatMimeTypeIndex;
        private static readonly object Padlock = new object();

        public static string GetMimeType(this Image source) { return source.GetCodec().MimeType; }

        public static string DefaultExtension(this ImageCodecInfo type) { return type.FilenameExtension.Split(';')[0].Substring(1).ToLower(); }

        public static ImageCodecInfo GetCodecByCode(string type) { return GetImageFormatMimeTypeIndex().FirstOrDefault(i => i.Value.FormatDescription.ToLower().Equals(type.ToLower())).Value; }
        public static ImageCodecInfo GetCodecByMimeType(string mimetype) { return GetImageFormatMimeTypeIndex().FirstOrDefault(i => i.Value.MimeType.ToLower().Equals(mimetype.ToLower())).Value; }

        public static ImageCodecInfo GetCodec(this Image source) { return GetImageFormatMimeTypeIndex()[source.RawFormat.Guid]; }

        public static Dictionary<Guid, ImageCodecInfo> GetImageFormatMimeTypeIndex()
        {
            lock (Padlock)
            {
                if (_imageFormatMimeTypeIndex != null) return _imageFormatMimeTypeIndex;

                var encoders = ImageCodecInfo.GetImageEncoders();
                _imageFormatMimeTypeIndex = new Dictionary<Guid, ImageCodecInfo>();
                foreach (var e in encoders) _imageFormatMimeTypeIndex.Add(e.FormatID, e);
                return _imageFormatMimeTypeIndex;
            }
        }
    }
}