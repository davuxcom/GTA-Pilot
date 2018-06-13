using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot.Extensions
{
    public static class ImageExtensions
    {
        public static Image<TColor, TDepth> SafeCopy<TColor, TDepth>(this Image<TColor, TDepth> img, Rectangle roi)
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

        public static double CountNonzeroAsPercentage<TColor, TDepth>(this Image<TColor, TDepth> img)
        where TColor : struct, IColor
        where TDepth : new()
        {
            var ct = img.CountNonzero()[0];

            var sz = img.Size.Width * img.Size.Height;

            return (double)ct / (double)sz;
        }

        public static Image<Gray, byte> DynLowInRange(this Image<Hsv, byte> img, DynHsv lower, Hsv higher)
        {
            var ret = img.InRange(lower.GetHsv(), higher);

            while (lower.RespondToResult(ret.CountNonzeroAsPercentage()))
            {
                ret = img.InRange(lower.GetHsv(), higher);

                var ct = ret.CountNonzeroAsPercentage();
                Trace.WriteLine("ADJUST: " + ct);
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
                CachedValue -= 2;
            }
            else if (diff < -0.005)
            {
                CachedValue += 2;
            }
            else
            {
                return false; // good enough!
            }
            return true;
        }

        public double Hue { get; set; }
        public double Satuation { get; set; }
        public double Value { get; set; }

        public double Count;
        public double CachedValue;
    }
}
