using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Nyan.Modules.Web.Tools.Media
{
    public static class Utilities
    {
        public static Image ResizeImage(Image image, int width, int height, EPosition focus = EPosition.Center)
        {
            var destRatio = ((float)width / height);
            var srcRatio = ((float)image.Width / image.Height);
            int intermediateWidth;
            int intermediateHeight;
            var widthOffSet = 0;
            var heightOffSet = 0;

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
                        heightOffSet = (intermediateHeight - height);
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
                        widthOffSet = (intermediateWidth - width);
                        break;
                    default:
                        widthOffSet = (intermediateWidth - width) / 2;
                        break;
                }
            }

            var bmPhoto = new Bitmap(intermediateWidth, intermediateHeight, PixelFormat.Format24bppRgb);

            var grp = Graphics.FromImage(bmPhoto);

            grp.InterpolationMode = InterpolationMode.HighQualityBicubic;
            grp.PixelOffsetMode = PixelOffsetMode.HighQuality;

            grp.DrawImage(image, 0, 0, intermediateWidth, intermediateHeight);

            var outputImage = new Bitmap(width, height);

            var section = new Rectangle(new Point(widthOffSet, heightOffSet), new Size(width, height));

            var preoutputGrp = Graphics.FromImage(outputImage);
            preoutputGrp.InterpolationMode = InterpolationMode.HighQualityBicubic;

            preoutputGrp.DrawImage(bmPhoto, 0, 0, section, GraphicsUnit.Pixel);

            preoutputGrp.Dispose();
            bmPhoto.Dispose();

            return outputImage;
        }

        public enum EPosition
        {
            Center,
            Top,
            Bottom,
            Left,
            Right
        }
    }
}