using Emgu.CV;
using Emgu.CV.Structure;
using System.Diagnostics;
using System.Drawing;

namespace GTAPilot.Extensions
{
    public static class ImageExtensions
    {
        // Same as Copy but with a check that the ROI is in the bounds, and if not, to adjust it such that
        // it doesn't copy past the edge of the image.
        public static Image<TColor, TDepth> SafeCopy<TColor, TDepth>(this Image<TColor, TDepth> img, Rectangle roi)
            where TColor : struct, IColor
            where TDepth : new()
        {
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

        public static double CountNonzeroAsPercentage<TColor, TDepth>(this Image<TColor, TDepth> img)
        where TColor : struct, IColor
        where TDepth : new()
        {
            var ct = img.CountNonzero()[0];
            var sz = img.Size.Width * img.Size.Height;
            return (double)ct / (double)sz;
        }

        // InRange but with a dynamic low bound that is tied to the DynHsv which tunes a value to reach
        // a certain number of ON pixels as a precent of image size.
        public static Image<Gray, byte> DynLowInRange(this Image<Hsv, byte> img, DynHsv lower, Hsv higher)
        {
            var ret = img.InRange(lower.GetHsv(), higher);

            int rev = 0;
            while (lower.RespondToResult(ret.CountNonzeroAsPercentage()))
            {
                ret = img.InRange(lower.GetHsv(), higher);

                if (++rev > 100)
                {
                    Trace.WriteLine("ERROR: DynLowInRange exceeded 100 attempts.");
                    break;
                }
            }
            return ret;
        }
    }

    public class DynHsv
    {
        public DynHsv(double hue, double satuation, double value, double targetCount, double cachedValueSeed)
        {
            Hue = hue;
            Satuation = satuation;
            Value = value;
            CachedValue = cachedValueSeed;
            Count = targetCount;
        }

        public Hsv GetHsv()
        {
            var hue = Hue;
            var saturation = Satuation;
            var value = Value;

            if (double.IsNaN(hue))
            {
                hue = CachedValue;
            }
            else if (double.IsNaN(saturation))
            {
                saturation = CachedValue;
            }
            else if (double.IsNaN(value))
            {
                value = CachedValue;
            }

            return new Hsv(hue, saturation, value);
        }

        public bool RespondToResult(double count)
        {
            var diff = Count - count;

            if (diff > 0.005)
            {
                CachedValue -= 1;
            }
            else if (diff < -0.005)
            {
                CachedValue += 1;
            }
            else
            {
                return false;
            }
            return true; // keep trying
        }

        public double Hue { get; set; }
        public double Satuation { get; set; }
        public double Value { get; set; }

        public double Count;
        public double CachedValue;
    }
}
