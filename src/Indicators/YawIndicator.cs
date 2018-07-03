using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GTAPilot.Indicators
{
    class YawIndicator : ISimpleIndicator
    {
        public class ExtendedData
        {
            public double LastN = double.NaN;
            public double LastE = double.NaN;
            public double LastS = double.NaN;
            public double LastW = double.NaN;
            public double Bias = double.NaN;
        }

        class BlobPack
        {
            public Image<Gray, byte> BlobImage;
            public double BlobRationAngle;
            public Rectangle BlobBox;
        }

        public double CachedTuningValue => dyn_lower.CachedValue;
        public double LastGoodValue => Timeline.Heading;

        DynHsv dyn_lower = new DynHsv(0, 0, double.NaN, 0.025, 100);

        public double ReadValue(IndicatorData data, DebugState debugState)
        {
            if (TryFindCircleInFullFrame(data, out var circ))
            {
                var focusRect = Math2.CropCircle(circ, 15);
                var focus = data.Frame.SafeCopy(focusRect);
                var focusHsv = focus.Convert<Hsv, byte>();
                var focusHsvText = focusHsv.DynLowInRange(dyn_lower, new Hsv(180, 255, 255));
                var focusHsvTriangleMask = focusHsv.InRange(new Hsv(0, 0, 0), new Hsv(180, 140, 255));
                var focusHsvTextOnly = focusHsvText.Copy(focusHsvTriangleMask);

                debugState.Add(focus);

                var blobs = Utils.DetectAndFilterBlobs(focusHsvTextOnly, 25, 250).
                    Where(b => b.Centroid.Y >= 5).OrderByDescending(b => b.Area).Take(4);

                var focusHsvOnlyBlobs = Utils.RemoveAllButBlobs(focusHsvTextOnly, blobs);
                debugState.Add(focusHsvOnlyBlobs);

                var parts = GetPacksFromImage(focusHsvOnlyBlobs, blobs, debugState);
                var ret = ComputeHeadingFromPacks(data.Id, parts, focus, debugState);

                if (!double.IsNaN(ret))
                {
                    // Adjust now that N and S are near-vertical and the blobs are more regular.
                    ret = RefineAngle(ret, focusHsvOnlyBlobs);
                    // Adjust based on the dots on the glareshield.
                    ret += GetGlareShieldSkewAngle(focusRect, data, debugState);
                    // Add a fixed cab skew to account for the vertical perspective.
                    ret -= 1;

                    ret = Math2.ClampAngle(ret);

                    // Proof for the debug info.
                    debugState.Add(focus.Rotate(ret, new Bgr(0, 0, 0)));

                    return CheckResultValidity(ret, data.Id);
                }
            }
            else
            {
                debugState.SetError("Couldn't find initial circle.");
            }
            return double.NaN;
        }

        private static bool TryFindCircleInFullFrame(IndicatorData data, out CircleF ret)
        {
            ret = default(CircleF);

            if (RollIndicator.TryFindRollCircleInFullFrame(data, out var circle))
            {
                circle.Center = new PointF(circle.Center.X + 1055, circle.Center.Y - 20);
                circle.Radius = 70;
                var firstCrop = Math2.CropCircle(circle, 40);
                var focus = data.Frame.SafeCopy(firstCrop);
                var focusHsv = focus.Convert<Hsv, byte>().PyrUp().PyrDown();

                var circles = CvInvoke.HoughCircles(focusHsv[2], HoughType.Gradient, 2.0, 80, 10, 80, 60, 80);
                if (circles.Length == 1)
                {
                    var circ = circles[0];
                    circles[0].Center = circles[0].Center.Add(firstCrop.Location);
                    circles[0].Radius = 64;
                    ret = circles[0];
                    return true;
                }
            }
            return false;
        }

        private IEnumerable<BlobPack> GetPacksFromImage(Image<Gray, byte> focus, IEnumerable<CvBlob> blobs, DebugState debugState)
        {
            var frame_center = new Point(focus.Width / 2, focus.Height / 2);
            var parts = new List<BlobPack>();

            foreach (var b in blobs)
            {
                b.BoundingBox.Inflate(2, 2);
                var rotationAngle = Math2.GetPolarHeadingFromLine(new LineSegment2D(Point.Round(b.Centroid), frame_center));

                var rotated_frame = focus.Rotate(-1 * rotationAngle, new Gray(0));
                var rotated_letter_only = rotated_frame.Copy(new Rectangle(rotated_frame.Width / 2 - 30, 0, 60, 60));

              //  rotated_letter_only = Utils.RemoveBlobs(rotated_letter_only, 1, 3);

                debugState.Add(rotated_letter_only);

                parts.Add(new BlobPack { BlobImage = rotated_letter_only, BlobRationAngle = rotationAngle, BlobBox = b.BoundingBox });
            }
            return parts;
        }

        private double RefineAngle(double resultAngle, Image<Gray, byte> focus)
        {
            var rotatedNorthUp = focus.Rotate(resultAngle, new Gray(0));

            // Re-rotate the frame now that N and S are vertical, so the centroids are in predictable
            // locations no matter what the inital angle was.
            var list = Utils.DetectAndFilterBlobs(rotatedNorthUp, 25, 250).OrderBy(b => b.Centroid.Y);
            if (list.Count() >= 2)
            {
                var verticalLine = new LineSegment2DF(new PointF(focus.Size.Width / 2, 0), new PointF(focus.Size.Width / 2, focus.Size.Height));
                var lineFromNorthToSouth = new LineSegment2DF(list.First().Centroid, list.Last().Centroid);
                var skewAngle = Math2.angleBetween2Lines(lineFromNorthToSouth, verticalLine) * (180 / Math.PI);
                // Divide by two since this is relative to the center of the line.
                resultAngle -= skewAngle / 2;
            }
            return resultAngle;
        }

        private double CheckResultValidity(double ret, int frameId)
        {
            var prev = Timeline.LatestFrame(d => d.Heading.Value, frameId);
            if (prev != null && !double.IsNaN(Timeline.Data[frameId].Heading.Value))
            {
                var dT = Timeline.Data[frameId].Heading.SecondsWhenComputed - prev.Heading.SecondsWhenComputed;
                if (dT < 1)
                {
                    var dX = Math2.DiffAngles(Timeline.Data[frameId].Heading.Value, prev.Heading.Value);
                    if (Math.Abs(dX) > 10)
                    {
                        // can't move more than 20 deg in one second
                        ret = double.NaN;
                    }
                }
            }
            return ret;
        }

        private double GetGlareShieldSkewAngle(Rectangle focusRect, IndicatorData data, DebugState debugState)
        {
            var topDotRect = new Rectangle(focusRect.Left - 160, focusRect.Top - 5, 40, 40);
            var bottomDotRect = new Rectangle(focusRect.Left - 170, focusRect.Bottom - 40, 60, 40);

            var topPointBlobs = GetDotLocationFromFullFrame(data.Frame, focusRect, topDotRect, isTop: true);
            var bottomPointBlobs = GetDotLocationFromFullFrame(data.Frame, focusRect, bottomDotRect, isTop: false);

            double a = double.NaN;
            if (topPointBlobs.Count > 0 && bottomPointBlobs.Count > 0)
            {
                var topPoint = topPointBlobs.First().Centroid.Add(topDotRect.Location);
                var bottomPoint = bottomPointBlobs.First().Centroid.Add(bottomDotRect.Location);

                a = Math2.GetPolarHeadingFromLine(topPoint, bottomPoint);
                if (a > 180)
                {
                    a = 360 - a;
                    a *= -1;
                }

                var lineRect = new Rectangle(focusRect.Left - 200, focusRect.Top - 50, 170, focusRect.Bottom + 50 - focusRect.Top);
                var lineImg = data.Frame.Copy(lineRect);

                var topInLineImg = new PointF(topPoint.X - lineRect.Location.X, topPoint.Y - lineRect.Location.Y);
                var bottomInLineImg = new PointF(bottomPoint.X - lineRect.Location.X, bottomPoint.Y - lineRect.Location.Y);

                var topRange = lineImg.Convert<Hsv, byte>(); //.InRange(new Hsv(HLow, SLow, VLow), new Hsv(HHigh, SHigh, VHigh));
                CvInvoke.Line(lineImg, topInLineImg.ToPoint(), bottomInLineImg.ToPoint(), new Bgr(Color.Yellow).MCvScalar, 1);
                debugState.Add(lineImg);
                debugState.Add(topRange);

                var dist = Math2.GetDistance(topPoint, bottomPoint);
                if (Math.Abs(a) > 8 || dist < 110 || dist > 120)
                {
                    var biasFrame = Timeline.LatestFrame(f => f.Heading.ForIndicatorUse == null ? double.NaN : ((ExtendedData)f.Heading.ForIndicatorUse).Bias, data.Id);
                    if (biasFrame != null)
                    {
                        a = ((ExtendedData)biasFrame.Heading.ForIndicatorUse).Bias;
                    }
                    else
                    {
                        debugState.SetError("Rejected due to dots angle out of bounds " + a);
                        return double.NaN;
                    }
                }
            }
            else
            {
                var biasFrame = Timeline.LatestFrame(f => f.Heading.ForIndicatorUse == null ? double.NaN : ((ExtendedData)f.Heading.ForIndicatorUse).Bias, data.Id);
                if (biasFrame != null)
                {
                    a = ((ExtendedData)biasFrame.Heading.ForIndicatorUse).Bias;
                }
                else
                {
                    debugState.SetError("Rejected due to dots angle out of bounds " + a);
                    return double.NaN;
                }
            }

            ((ExtendedData)Timeline.Data[data.Id].Heading.ForIndicatorUse).Bias = a;
            return a / 2;
        }

        private List<CvBlob> GetDotLocationFromFullFrame(Image<Bgr, byte> frame, Rectangle focusRect, Rectangle dotRect, bool isTop)
        {
            var topDot = frame.SafeCopy(dotRect);
            var topHsv = topDot.Convert<Hsv, byte>();
            var topRange = topHsv.InRange(new Hsv(0, 0, 80), new Hsv(180, 255, 150));

            var ret = Utils.DetectAndFilterBlobs(topRange, 5, 18).ToList();
            if (isTop)
            {
                return ret.OrderByDescending(b => b.Centroid.Y).ToList();
            }
            else
            {
                return ret.OrderBy(b => b.Centroid.Y).ToList();
            }
        }

        private string GetText(Image<Gray, byte> img, DebugState debugState)
        {
            var str = Utils.ReadTextFromImage(img, debugState);


            if (str == "VI") str = "W";
            if (str == "VV") str = "W";
            if (str == "VY") str = "W";
            if (str == "XV") str = "W";
            if (str == "\\N") str = "W";
            if (str == "5") str = "S";
            if (str == "8") str = "S";
            if (str == "3") str = "S";
            if (str == "9") str = "S";
            if (str == "G") str = "S";
            if (str == "$") str = "S";
            if (str == "U") str = "N";
            if (str == "M") str = "N";
            if (str == "H") str = "N";
            if (str == "I") str = "N";
            if (str == "II") str = "N";
            if (str == "ﬂ") str = "N";
            if (str == "ﬁ") str = "N";
            if (str == "F") str = "E";
            if (str == "É") str = "E";
            if (str == "=") str = "E";

            return str;
        }

        string ResolveTextFromPreviousFrame(int frameId, double angle)
        {
            var frame = Timeline.Data[frameId];
            var lastFrame = Timeline.LatestFrame(f => f.Heading.Value, frameId - 1);

            if (lastFrame != null && (frame.Seconds - lastFrame.Seconds) < 1)
            {
                var lastFrameExtended = (ExtendedData)lastFrame.Heading.ForIndicatorUse;

                var dN = Math.Abs(Math2.DiffAngles(angle, lastFrameExtended.LastN));
                var dE = Math.Abs(Math2.DiffAngles(angle, lastFrameExtended.LastE));
                var dS = Math.Abs(Math2.DiffAngles(angle, lastFrameExtended.LastS));
                var dW = Math.Abs(Math2.DiffAngles(angle, lastFrameExtended.LastW));

                var T = 15; // Degrees motion
                var tx = 0;

                if (dN < T) tx++;
                if (dE < T) tx++;
                if (dS < T) tx++;
                if (dW < T) tx++;

                if (tx == 1)
                {
                    if (dN < T) return "N";
                    if (dE < T) return "E";
                    if (dS < T) return "S";
                    if (dW < T) return "W";
                }
            }
            return null;
        }

        double ComputeHeadingFromPacks(int frameId, IEnumerable<BlobPack> packs, Image<Bgr, Byte> compass_frame, DebugState debugState)
        {
            var frame = Timeline.Data[frameId];
            var my_extended = new ExtendedData();
            frame.Heading.ForIndicatorUse = my_extended;

            var choices = new List<Tuple<double, double, string, Image<Gray, byte>>>();
            double unused_angle = 0;

            var p = packs.OrderByDescending(px => px.BlobRationAngle);
            foreach (var pack in packs)
            {
                var b = pack.BlobBox;
                b.Inflate(2, 2);

                var small_angle = pack.BlobRationAngle;
                var str = ResolveTextFromPreviousFrame(frameId, small_angle);

                if (string.IsNullOrWhiteSpace(str))
                {
                    str = GetText(pack.BlobImage, debugState);
                }

                if (str == "N" || str == "E" || str == "S" || str == "W")
                {
                    small_angle = 360 - Math.Abs(small_angle);
                    double new_heading = 0;
                    switch (str)
                    {
                        case "N":
                            my_extended.LastN = pack.BlobRationAngle;
                            new_heading = small_angle;
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Blue).MCvScalar, 1);
                            break;
                        case "E":
                            my_extended.LastE = pack.BlobRationAngle;
                            new_heading = (small_angle + 90);
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Yellow).MCvScalar, 1);
                            break;
                        case "S":
                            my_extended.LastS = pack.BlobRationAngle;
                            new_heading = (small_angle + 180);
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Red).MCvScalar, 1);
                            break;
                        case "W":
                            my_extended.LastW = pack.BlobRationAngle;
                            new_heading = (small_angle + 270);
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Lime).MCvScalar, 1);
                            break;
                    }

                    new_heading = Math2.ClampAngle(new_heading);
                    choices.Add(new Tuple<double, double, string, Image<Gray, byte>>(new_heading, small_angle, str, pack.BlobImage));
                }
                else
                {
                    unused_angle = pack.BlobRationAngle;
                }
            }

            // Fill in exactly one missing quadrant.
            if (choices.Count == 3 && packs.Count() == 4)
            {
                var letters = new List<string>() { "N", "E", "S", "W" };
                foreach (var c in choices) letters.Remove(c.Item3);

                var o_angle = unused_angle;
                unused_angle = 360 - Math.Abs(unused_angle);

                double new_heading = 0;
                var str = letters.First();
                switch (str)
                {
                    case "N":
                        my_extended.LastN = o_angle;
                        new_heading = unused_angle;
                        break;
                    case "E":
                        my_extended.LastE = o_angle;
                        new_heading = (unused_angle + 90);
                        break;
                    case "S":
                        my_extended.LastS = o_angle;
                        new_heading = (unused_angle + 180);
                        break;
                    case "W":
                        my_extended.LastW = o_angle;
                        new_heading = (unused_angle + 270);
                        break;
                }

                new_heading = Math2.ClampAngle(new_heading);
                choices.Add(new Tuple<double, double, string, Image<Gray, byte>>(new_heading, (int)unused_angle, str, null));
            }
            
            if (choices.Count == 4)
            {
                // Exclude invalid combinations
                if (choices.Where(ct => ct.Item3 == "N").Count() > 1)
                {
                    debugState.SetError("Bad N");
                    return double.NaN;
                }
                if (choices.Where(ct => ct.Item3 == "E").Count() > 1)
                {
                    debugState.SetError("Bad E");
                    return double.NaN;
                }
                if (choices.Where(ct => ct.Item3 == "S").Count() > 1)
                {
                    debugState.SetError("Bad S");
                    return double.NaN;
                }
                if (choices.Where(ct => ct.Item3 == "W").Count() > 1)
                {
                    debugState.SetError("Bad W");
                    return double.NaN;
                }

                var p1 = Math2.AddAngles(choices[0].Item1, choices[1].Item1);
                var p2 = Math2.AddAngles(choices[2].Item1, choices[3].Item1);
                return Math2.AddAngles(p1, p2);
            }
            else
            {
                debugState.SetError($"Bad choices {choices.Count}");
            }
            return double.NaN;
        }
    }
}
