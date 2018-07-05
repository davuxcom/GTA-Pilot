using Emgu.CV;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System.Drawing;
using System.Linq;

namespace GTAPilot.Indicators
{
    class LandingGearIndicator : ISimpleIndicator
    {
        public double CachedTuningValue => dyn_lower.CachedValue;
        public double LastGoodValue => Timeline.Altitude;

        DynHsv dyn_lower = new DynHsv(0, 0, double.NaN, 0.008, 100);

        public double ReadValue(IndicatorData data, DebugState debugState)
        {
            if (RollIndicator.TryFindRollCircleInFullFrame(data, out var circle))
            {
                var firstCrop = new Rectangle((int)circle.Center.X + 500, (int)circle.Center.Y - 180, 100, 150);
                var focus = data.Frame.SafeCopy(firstCrop);

                var vs_blackImg = focus.Convert<Hsv, byte>().InRange(new Hsv(0, 120, 0), new Hsv(180, 255, 255));


                var blobs = Utils.DetectAndFilterBlobs(vs_blackImg.PyrUp().PyrDown(), 1500, 2500);
                if (blobs.Any())
                {
                    var landingGearFrame = focus.Copy(blobs.First().BoundingBox);

                    var hsv = landingGearFrame.Convert<Hsv, byte>();
                    var black_img = hsv[2].InRange(new Gray(140), new Gray(255));
                    debugState.Add(landingGearFrame);
                    debugState.Add(black_img);

                    blobs = Utils.DetectAndFilterBlobs(black_img, 200, 1500);
                    if (blobs.Any())
                    {
                        var blob = blobs.First();
                        var ret = (landingGearFrame.Height / 2) - blob.Centroid.Y;

                        if (ret > 4) return 1;
                        else if (ret < -4) return -1;
                    }
                }
            }
            return double.NaN;
        }
    }
}