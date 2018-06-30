using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace GTAPilot.Indicators
{
    class YawIndicator : ISimpleIndicator
    {
        public class CompassExtendedFrame
        {
            public double LastN = double.NaN;
            public double LastE = double.NaN;
            public double LastS = double.NaN;
            public double LastW = double.NaN;
            public double Bias = double.NaN;
        }

        class CompassPack
        {
            public Image<Gray, byte> Item1;
            public double Item2;
            public Rectangle BlobBox;
            public double BlobArea;
        }

        public static int HLow { get; set; }
        public static int SLow { get; set; }
        public static int VLow { get; set; }
        public static int HHigh { get; set; }
        public static int SHigh { get; set; }
        public static int VHigh { get; set; }

        static YawIndicator()
        {
            HHigh = 255;
            SHigh = 255;
            VHigh = 255;
        }

        public double CachedTuningValue => dyn_lower.CachedValue;
        public double LastGoodValue => Timeline.Heading;

        DynHsv dyn_lower = new DynHsv(0, 0, double.NaN, 0.025, 100);

        public double ReadValue(IndicatorData data, ref object[] debugState)
        {
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
                    circ.Center = circles[0].Center.Add(firstCrop.Location);
                    circ.Radius = 64;

                    var focusRect = Math2.CropCircle(circ, 15);
                    focus = data.Frame.SafeCopy(focusRect);

                    debugState[0] = focus;

                    focusHsv = focus.Convert<Hsv, byte>();

                    var focusHsvText = focusHsv.DynLowInRange(dyn_lower, new Hsv(180, 255, 255));
                    var focusHsvTriangleMask = focusHsv.InRange(new Hsv(0, 0, 0), new Hsv(180, 140, 255));
                    var focusHsvTextOnly = focusHsvText.Copy(focusHsvTriangleMask);
                    var focusHsvTextOnlyMarkedUp = focusHsvTextOnly.Convert<Bgr, byte>();

                    debugState[1] = focusHsvTextOnlyMarkedUp;

                    var blobs = PerThreadUtils.DetectAndFilterBlobs(focusHsvTextOnly, 25, 250).
                        Where(b => b.Centroid.Y >= 5).OrderByDescending(b => b.Area).Take(4).ToList();

                    Mat blobMask = new Mat(focusHsv.Size, DepthType.Cv8U, 3);
                    blobMask.SetTo(new MCvScalar(1));

                    foreach (var b in blobs)
                    {
                        CvInvoke.Rectangle(blobMask, b.BoundingBox, new Bgr(Color.White).MCvScalar, -1);
                        CvInvoke.Rectangle(focusHsvTextOnlyMarkedUp, b.BoundingBox, new Bgr(Color.Red).MCvScalar, -1);
                    }

                    var focusHsvOnlyBlobs = focusHsvTextOnly.Copy(blobMask.ToImage<Gray, byte>());
                    debugState[2] = focusHsvOnlyBlobs;

                    var frame_center = new Point(focus.Width / 2, focus.Height / 2);

                    var vertical_center_line = new LineSegment2D(new Point((focus.Width / 2), 0), new Point((focus.Width / 2), (focus.Height / 2)));
                    var parts = new List<CompassPack>();

                    foreach (var b in blobs)
                    {
                        b.BoundingBox.Inflate(2, 2);

                        var lineFromBlobToCenter = new LineSegment2D(Point.Round(b.Centroid), frame_center);
                        var ang = Math2.angleBetween2Lines(lineFromBlobToCenter, vertical_center_line);
                        var rotationAngle = Math2.FixAngle(ang, frame_center, Point.Round(b.Centroid));

                        var rotated_frame = focusHsvOnlyBlobs.Rotate(-1 * rotationAngle, new Gray(0));
                        var rotated_letter_only = rotated_frame.Copy(new Rectangle(rotated_frame.Width / 2 - 30, 0, 60, 60));

                        if (debugState[3] == null)
                        {
                            debugState[3] = rotated_letter_only;
                        }

                        parts.Add(new CompassPack { Item1 = rotated_letter_only, Item2 = rotationAngle, BlobBox = b.BoundingBox, BlobArea = b.Area });
                    }

                    var ret = CompassProcFrame(data.Id, parts, focus, ref debugState);
                    if (!double.IsNaN(ret))
                    {
                        var compass_frame = focusHsvOnlyBlobs.Rotate(ret, new Gray(0));

                        // Re-rotate the frame now that N and S are vertical, so the centroids are in predictable
                        // locations no matter what the inital angle was.
                        var list = PerThreadUtils.DetectAndFilterBlobs(compass_frame, 25, 250).OrderBy(b => b.Centroid.Y);

                        var top = list.First().Centroid;
                        var bottom = list.Last().Centroid;

                        var verticalLine = new LineSegment2DF(new PointF(focus.Size.Width / 2, 0), new PointF(focus.Size.Width / 2, focus.Size.Height));
                        var lineFromNorthToSouth = new LineSegment2DF(top, bottom);
                        var skewAngle = Math2.angleBetween2Lines(lineFromNorthToSouth, verticalLine);
                        skewAngle = (skewAngle * (180 / Math.PI));

                        ret -= skewAngle / 2;

                        compass_frame = focusHsvOnlyBlobs.Rotate(ret, new Gray(0));

                        var topDotRect = new Rectangle(focusRect.Left - 160, focusRect.Top - 5, 40, 40);
                        var bottomDotRect = new Rectangle(focusRect.Left - 170, focusRect.Bottom - 40, 60, 40);

                        var topPointBlobs = GetDotLocationFromFrame(data.Frame, focusRect, topDotRect, isTop: true);
                        var bottomPointBlobs = GetDotLocationFromFrame(data.Frame, focusRect, bottomDotRect, isTop: false);

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

                            debugState[3] = lineImg;
                            debugState[4] = topRange;

                            var dist = Math2.GetDistance(topPoint, bottomPoint);
                            if (Math.Abs(a) > 8 || dist < 110 || dist > 120)
                            {
                                var biasFrame = Timeline.LatestFrame(f => f.Heading.ForIndicatorUse == null ? double.NaN : ((CompassExtendedFrame)f.Heading.ForIndicatorUse).Bias, data.Id);
                                if (biasFrame != null)
                                {
                                    a = ((CompassExtendedFrame)biasFrame.Heading.ForIndicatorUse).Bias;
                                }
                                else
                                {
                                    TraceLine("Rejected due to dots angle out of bounds " + a);
                                    return double.NaN;
                                }
                            }
                        }
                        else
                        {
                            var biasFrame = Timeline.LatestFrame(f => f.Heading.ForIndicatorUse == null ? double.NaN : ((CompassExtendedFrame)f.Heading.ForIndicatorUse).Bias, data.Id);
                            if (biasFrame != null)
                            {
                                a = ((CompassExtendedFrame)biasFrame.Heading.ForIndicatorUse).Bias;
                            }
                            else
                            {
                                TraceLine("Rejected due to dots angle out of bounds " + a);
                                return double.NaN;
                            }
                        }

                        ret += a / 2;
                        ((CompassExtendedFrame)Timeline.Data[data.Id].Heading.ForIndicatorUse).Bias = a;

                        // Cab skew
                        ret -= 1;

                        if (ret < 0) ret = 360 - ret;
                        if (ret > 360) ret -= 360;

                        return ret;
                    }
                    else
                    {
                        //TraceLine("PROC returned null");
                    }
                }
                else
                {
                    TraceLine("Couldn't find first level circle");
                }
            }
            else
            {
                TraceLine("Couldn't find roll indicator");
            }
            return double.NaN;
        }

        private List<CvBlob> GetDotLocationFromFrame(Image<Bgr, byte> frame, Rectangle focusRect, Rectangle dotRect, bool isTop)
        {
            var topDot = frame.SafeCopy(dotRect);
            var topHsv = topDot.Convert<Hsv, byte>();
            var topRange = topHsv.InRange(new Hsv(0, 0, 80), new Hsv(180, 255, 150));

            var ret = PerThreadUtils.DetectAndFilterBlobs(topRange, 5, 18).ToList();
            if (isTop)
            {
                return ret.OrderByDescending(b => b.Centroid.Y).ToList();
            }
            else
            {
                return ret.OrderBy(b => b.Centroid.Y).ToList();
            }
        }

        private string GetText(Image<Gray, byte> img)
        {
            var ocr = PerThreadUtils.GetTesseract();
            ocr.SetImage(img);
            ocr.Recognize();

            var str = "";
            foreach (var c in ocr.GetCharacters())
            {
                str += c.Text.Trim().ToUpper();
            }
            
            str = str.Replace("'", "").Replace(".", "").Replace("‘", "").Replace("‘", "’").Replace("\"", "").Replace(",", "").Replace(":", "");
            
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

        double CompassProcFrame(int frameId, List<CompassPack> packs, Image<Bgr, Byte> compass_frame, ref object[] debugState)
        {
            var frame = Timeline.Data[frameId];
            var lastFrame = Timeline.LatestFrame(f => f.Heading.Value, frameId - 1);

            var choices = new List<Tuple<double, double, string, Image<Gray, byte>, double>>();
            var others = new List<string>();
            double unused_angle = 0;

            var p = packs.OrderByDescending(px => px.Item2);
            foreach (var pack in packs)
            {
                var str = "";
                var small_angle = pack.Item2;
                var b = pack.BlobBox;

                if (str != "N" &&
                    str != "E" &&
                    str != "S" &&
                    str != "W")
                {
                    //  TraceLine("CHO : " + str);
                }

                // If 'str' was not NESW, look back and copy if the position is close enough to only one NESW.
                if (lastFrame != null && (frame.Seconds - lastFrame.Seconds) < 1)
                {
                    var extended = (CompassExtendedFrame)lastFrame.Heading.ForIndicatorUse;
                    if (str != "N" &&
                        str != "E" &&
                        str != "S" &&
                        str != "W")
                    {
                        var dN = Math.Abs(Math2.DiffAngles(pack.Item2, extended.LastN));
                        var dE = Math.Abs(Math2.DiffAngles(pack.Item2, extended.LastE));
                        var dS = Math.Abs(Math2.DiffAngles(pack.Item2, extended.LastS));
                        var dW = Math.Abs(Math2.DiffAngles(pack.Item2, extended.LastW));

                        var T = 15;
                        var tx = 0;

                        if (dN < T) tx++;
                        if (dE < T) tx++;
                        if (dS < T) tx++;
                        if (dW < T) tx++;

                        if (tx == 1)
                        {
                            if (dN < T) str = "N";
                            if (dE < T) str = "E";
                            if (dS < T) str = "S";
                            if (dW < T) str = "W";
                        }
                        else
                        {
                           // TraceLine($"carry failed {tx}");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(str))
                {
                    str = GetText(pack.Item1);
                }

                if (str == "N" ||
                    str == "E" ||
                    str == "S" ||
                    str == "W")
                {
                    small_angle = 360 - Math.Abs(small_angle);

                    double new_heading = 0;

                    b.Inflate(2, 2);

                    var extended = frame.Heading.ForIndicatorUse == null ? new CompassExtendedFrame() : (CompassExtendedFrame)frame.Heading.ForIndicatorUse;
                    frame.Heading.ForIndicatorUse = extended;

                    switch (str)
                    {
                        case "N":
                            extended.LastN = pack.Item2;
                            new_heading = small_angle;
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Blue).MCvScalar, 1);

                            break;
                        case "E":
                            extended.LastE = pack.Item2;
                            new_heading = (small_angle + 90);
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Yellow).MCvScalar, 1);

                            break;
                        case "S":
                            extended.LastS = pack.Item2;
                            new_heading = (small_angle + 180);
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Red).MCvScalar, 1);

                            break;
                        case "W":
                            extended.LastW = pack.Item2;
                            new_heading = (small_angle + 270);
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Lime).MCvScalar, 1);

                            break;
                    }

                    while (new_heading > 360)
                    {
                        new_heading = new_heading - 360;
                    }
                    if (new_heading < 0) new_heading += 360;

                    choices.Add(new Tuple<double, double, string, Image<Gray, byte>, double>(new_heading, small_angle, str, pack.Item1, pack.BlobArea));
                }
                else
                {
                    unused_angle = pack.Item2;

                    others.Add(str);

                }
            }

            // Fill in exactly one missing quadrant.
            if (choices.Count == 3 && packs.Count == 4)
            {

                var letters = new List<string>() { "N", "E", "S", "W" };


                foreach (var c in choices) letters.Remove(c.Item3);


                var str = letters.First();

                var o_angle = unused_angle;
                unused_angle = 360 - Math.Abs(unused_angle);

                var extended = (CompassExtendedFrame)frame.Heading.ForIndicatorUse;

                double new_heading = 0;
                switch (str)
                {
                    case "N":
                        extended.LastN = o_angle;
                        new_heading = unused_angle;

                        break;
                    case "E":
                        extended.LastE = o_angle;

                        new_heading = (unused_angle + 90);

                        break;
                    case "S":
                        extended.LastS = o_angle;

                        new_heading = (unused_angle + 180);

                        break;
                    case "W":
                        extended.LastW = o_angle;

                        new_heading = (unused_angle + 270);

                        break;
                }


                while (new_heading > 360)
                {
                    new_heading = new_heading - 360;
                }

                if (new_heading < 0) new_heading += 360;

                choices.Add(new Tuple<double, double, string, Image<Gray, byte>, double>(new_heading, (int)unused_angle, str, null, 0));
            }
            
            if (choices.Count == 4)
            {
                choices = choices.OrderBy(cx => cx.Item5).ToList();

                // exclude bad combinations
                if (choices.Where(ct => ct.Item3 == "N").Count() > 1)
                {
                    TraceLine("Bad N");
                    return double.NaN;
                }
                if (choices.Where(ct => ct.Item3 == "E").Count() > 1)
                {
                    TraceLine("Bad E");

                    return double.NaN;
                }
                if (choices.Where(ct => ct.Item3 == "S").Count() > 1)
                {
                    TraceLine("Bad S");

                    return double.NaN;
                }
                if (choices.Where(ct => ct.Item3 == "W").Count() > 1)
                {
                    TraceLine("Bad W");

                    return double.NaN;
                }

                var p1 = Math2.AddAngles(choices[0].Item1, choices[1].Item1);
                var p2 = Math2.AddAngles(choices[2].Item1, choices[3].Item1);
                return Math2.AddAngles(p1, p2);
            }
            else
            {
                TraceLine($"Bad choices {choices.Count}");
            }
            return double.NaN;
        }

        private void TraceLine(string msg)
        {
            //
        }
    }
}
