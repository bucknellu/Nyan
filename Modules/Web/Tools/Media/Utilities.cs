using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Nyan.Core;
using Nyan.Core.Extensions;
using Nyan.Core.Media;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.Tools.Media
{
    public static class Utilities
    {
        public enum EPosition
        {
            Center,
            Top,
            Bottom,
            Left,
            Right
        }

        public static Image ResizeImage(Image image, int width, int height, EPosition focus = EPosition.Center, bool crop = true)
        {
            var destRatio = (float)width / height;
            var srcRatio = (float)image.Width / image.Height;
            int intermediateWidth;
            int intermediateHeight;
            var widthOffSet = 0;
            var heightOffSet = 0;

            if (crop)
            {
                if (srcRatio <= destRatio)
                {
                    intermediateWidth = width;
                    intermediateHeight = (int)(intermediateWidth / srcRatio);

                    switch (focus)
                    {
                        case EPosition.Top:
                            heightOffSet = 0;
                            break;
                        case EPosition.Bottom:
                            heightOffSet = intermediateHeight - height;
                            break;
                        default:
                            heightOffSet = (intermediateHeight - height) / 2;
                            break;
                    }
                }
                else
                {
                    intermediateHeight = height;
                    intermediateWidth = (int)(intermediateHeight * srcRatio);

                    switch (focus)
                    {
                        case EPosition.Left:
                            widthOffSet = 0;
                            break;
                        case EPosition.Right:
                            widthOffSet = intermediateWidth - width;
                            break;
                        default:
                            widthOffSet = (intermediateWidth - width) / 2;
                            break;
                    }
                }
            }
            else
            {
                if (srcRatio <= destRatio) // Destiny is wider than source.
                {

                    intermediateHeight = height;
                    intermediateWidth = (int)(intermediateHeight * srcRatio);

                    switch (focus)
                    {
                        case EPosition.Left:
                            widthOffSet = 0;
                            break;
                        case EPosition.Right:
                            widthOffSet = intermediateWidth - width;
                            break;
                        default:
                            widthOffSet = (intermediateWidth - width) / 2;
                            break;
                    }
                }
                else
                {
                    intermediateWidth = width;
                    intermediateHeight = (int)(intermediateWidth / srcRatio);

                    switch (focus)
                    {
                        case EPosition.Top:
                            heightOffSet = 0;
                            break;
                        case EPosition.Bottom:
                            heightOffSet = intermediateHeight - height;
                            break;
                        default:
                            heightOffSet = (intermediateHeight - height) / 2;
                            break;
                    }
                }
            }

            var bmPhoto = new Bitmap(intermediateWidth, intermediateHeight, PixelFormat.Format32bppPArgb);

            var grp = Graphics.FromImage(bmPhoto);
            grp.Clear(Color.Transparent);

            grp.InterpolationMode = InterpolationMode.HighQualityBicubic;
            grp.PixelOffsetMode = PixelOffsetMode.HighQuality;

            grp.DrawImage(image, 0, 0, intermediateWidth, intermediateHeight);

            var outputImage = new Bitmap(width, height);

            var section = new Rectangle(new Point(widthOffSet, heightOffSet), new Size(width, height));

            var preoutputGrp = Graphics.FromImage(outputImage);
            preoutputGrp.Clear(Color.Transparent);

            preoutputGrp.InterpolationMode = InterpolationMode.HighQualityBicubic;

            preoutputGrp.DrawImage(bmPhoto, 0, 0, section, GraphicsUnit.Pixel);

            preoutputGrp.Dispose();
            bmPhoto.Dispose();

            return outputImage;
        }

        public static string GetImageResourcePath(string url)
        {
            if (url.IndexOf("http", StringComparison.Ordinal) == -1) url = $"http://{url}";

            //can contain the text http and contain incorrect uri scheme.
            var isUrl = Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!isUrl) throw new ArgumentException($"Parameter is invalid: url ({url})");

            try
            {
                var cacheDir = Configuration.DataDirectory + "\\cache\\media\\sources";

                var cacheNameStandard = "media-external-" + url.Md5Hash();

                var cacheFileNameSource = cacheDir + "\\" + cacheNameStandard;
                if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

                var nameFormat = cacheNameStandard + ".*";

                var possibleFiles = Directory.GetFiles(cacheDir, nameFormat).ToList();

                if (possibleFiles.Count > 0) return possibleFiles[0];

                //connect via web client and get the photo at the specified url

                Current.Log.Add("MEDIASOURCE fetch: " + url, Message.EContentType.Info);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

                using (var httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    var stream = httpWebReponse.GetResponseStream();
                    if (stream == null) return null;

                    var ms = new MemoryStream();
                    stream.CopyTo(ms);

                    var image = Image.FromStream(ms);
                    if (image.Width * image.Height > 67108864) throw new ArgumentException("Combined size is invalid. Limit pixel count to 67108864, or 64Mb (width x height).");

                    var fileDest = cacheFileNameSource + image.GetCodec().DefaultExtension();

                    Current.Log.Add("MEDIASOURCE cache: " + fileDest, Message.EContentType.MoreInfo);

                    ms.Position = 0;

                    using (var fs = new FileStream(fileDest, FileMode.OpenOrCreate))
                    {
                        ms.CopyTo(fs);
                        fs.Flush();
                    }

                    return fileDest;
                }
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                return null;
            }
        }

        public static Image GetFormattedImageResource(string url, int? width, int? height, bool crop, string format, string position) { return GetFormattedImageResourcePath(url, width, height, crop, format, position).FromPathToImage(); }

        public static string GetFormattedImageResourcePath(string url, int? width, int? height, bool crop, string format, string position)
        {
            if ((width == null) & (height == null) & (format == null)) // This is just a proxy, then: Return the source image.
                return GetImageResourcePath(url);

            var cacheDir = Configuration.DataDirectory + "\\cache\\media\\formatted";

            format = format?.Trim().ToLower();

            ImageCodecInfo codec;

            if (!Enum.TryParse(position, out EPosition pos)) pos = EPosition.Center;

            if (position != null)
                try { pos = (EPosition)Enum.Parse(typeof(EPosition), position); } catch { pos = EPosition.Center; }

            // Ok, we have some specs.

            if (format == null)
            {
                var preCodec = MimeMapping.GetMimeMapping(url);

                // Let's save some processing and try first to obtain the MIME type from the URL.
                codec = Core.Media.Utilities.GetCodecByMimeType(preCodec);
            }
            else { codec = Core.Media.Utilities.GetCodecByCode(format); }

            Image sourceImage = null;

            if (codec == null)
            {
                Current.Log.Add($"MEDIASOURCE Codec not found: format {format}, url {url}", Message.EContentType.Warning);
                sourceImage = GetImageResourcePath(url).FromPathToImage();
                codec = sourceImage.GetCodec();
            }

            if (width == null || height == null)
                if (sourceImage == null)
                    sourceImage = GetImageResourcePath(url).FromPathToImage();

            if (width == null) width = sourceImage.Width;
            if (height == null) height = sourceImage.Height;

            var dimensionsPart
                = "-w" + width
                       + "-h" + height
                       + "-c" + crop
                       + "-f" + pos;

            var md5Url = url.Md5Hash();
            var cacheFileNameFormatted = cacheDir + "\\" + md5Url + dimensionsPart + codec.DefaultExtension();


            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            if (File.Exists(cacheFileNameFormatted)) return cacheFileNameFormatted;


            if (sourceImage == null) sourceImage = GetImageResourcePath(url).FromPathToImage();

            ResizeImage(sourceImage, width.Value, height.Value, pos, crop).Save(cacheFileNameFormatted, codec, null);

            Current.Log.Add("MEDIASOURCE FORMATTED: " + cacheFileNameFormatted, Message.EContentType.MoreInfo);

            return cacheFileNameFormatted;
        }
    }
}