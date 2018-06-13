using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot.Extensions
{
    public static class ImageExtensions
    {
        public static Image<TColor, TDepth> SafeCopy<TColor,TDepth>(this Image<TColor, TDepth> img, Rectangle roi) 
            where TColor : struct, IColor
            where TDepth : new()
        {
            // Verify roi fits
            if (roi.Location.X + roi.Width > img.Size.Width)
            {
                roi.Location = new Point(img.Size.Width - roi.Width, roi.Location.Y);
            }

            if (roi.Location.Y + roi.Height > img.Size.Height)
            {
                roi.Location = new Point(roi.Location.X, img.Size.Height - roi.Height);
            }

            return img.Copy(roi);
        }
    }
}
