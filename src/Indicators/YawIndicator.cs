using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
                circle.Center = new PointF(circle.Center.X + 1050, circle.Center.Y - 20);
                circle.Radius = 70;
                var firstCrop = Math2.CropCircle(circle, 40);
                var focus = data.Frame.SafeCopy(firstCrop);
                var vs_hsv = focus.Convert<Hsv, byte>().PyrUp().PyrDown();

                var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 80, 10, 80, 60, 80);
                if (circles.Length == 1)
                {
                    var circ = circles[0];
                    circ.Center = circles[0].Center.Add(firstCrop.Location);
                    circ.Radius = 64;


                    var focusRect = Math2.CropCircle(circ, 15);
                    focus = data.Frame.SafeCopy(focusRect);

                    debugState[0] = focus;

                    vs_hsv = focus.Convert<Hsv, byte>();

                    var vs_text = vs_hsv.DynLowInRange(dyn_lower, new Hsv(180, 255, 255));
                    var vs_mask = vs_hsv.InRange(new Hsv(0, 0, 0), new Hsv(180, 140, 255));
                    var vs_textonly = vs_text.Copy(vs_mask);
                    var markedup_textonly = vs_textonly.Convert<Bgr, byte>();

                    debugState[1] = markedup_textonly;

                    CvBlobs blobs = new CvBlobs();
                    GetBlobDetector().Detect(vs_textonly, blobs);
                    blobs.FilterByArea(25, 250);
                    Mat blobMask = new Mat(vs_hsv.Size, DepthType.Cv8U, 3);
                    blobMask.SetTo(new MCvScalar(1));

                    // Select 4 largest
                    var list_blobs = new List<CvBlob>();
                    foreach (var b in blobs) list_blobs.Add(b.Value);

                    for (var i = list_blobs.Count - 1; i >= 0; i--)
                    {
                        var b = list_blobs[i];
                        if (b.Centroid.Y < 5)
                        {
                            list_blobs.Remove(b);
                        }
                    }

                    list_blobs = list_blobs.OrderByDescending(bx => bx.Area).Take(Math.Min(4, blobs.Count)).ToList();


                    for (int i_blob = 0; i_blob < list_blobs.Count; i_blob++)
                    {
                        CvBlob b = list_blobs[i_blob];
                        CvInvoke.Rectangle(blobMask, b.BoundingBox, new Bgr(Color.White).MCvScalar, -1);
                        CvInvoke.Rectangle(markedup_textonly, b.BoundingBox, new Bgr(Color.Red).MCvScalar, -1);
                    }


                    if (list_blobs.Count == 4)
                    {

                        var results = new List<Tuple<PointF, PointF, double>>();

                        foreach (var b_o in list_blobs)
                        {
                            foreach (var b_i in list_blobs)
                            {
                                results.Add(new Tuple<PointF, PointF, double>(b_o.Centroid, b_i.Centroid, Math2.GetDistance(b_o.Centroid, b_i.Centroid)));
                            }
                        }

                        results = results.OrderByDescending(p => p.Item3).Take(4).ToList();

                        reset_1:
                        foreach (var t in results)
                        {
                            foreach (var x in results)
                            {
                                if (t.Item1 == x.Item2)
                                {
                                    results.Remove(x);
                                    goto reset_1;
                                }
                            }
                        }
                        //  LastAction = "Checking Results";
                        if (results.Count != 2) return double.NaN;
                        //  LastAction = "Have Results";

                    }
                    else
                    {
                        return double.NaN; // no blobs
                    }

                    var parts = new List<CompassPack>();
                    var only_blobs = vs_textonly.Copy(blobMask.ToImage<Gray, byte>());

                    debugState[2] = only_blobs;


                    Point v_center_point = new Point(focus.Width / 2, focus.Height / 2);
                    Rectangle lastRect = default(Rectangle);
                    Size lastRotatedFrame = default(Size);


                    LineSegment2D baseLine = new LineSegment2D(new Point((focus.Width / 2), 0), new Point((focus.Width / 2), (focus.Height / 2)));

                    int ix = 0;
                    for (int i_blob = 0; i_blob < list_blobs.Count; i_blob++)
                    {
                        ix++;

                        CvBlob b = list_blobs[i_blob];

                        var box = b.BoundingBox;
                        box.Inflate(2, 2);
                        try
                        {
                            LineSegment2D small_line = new LineSegment2D(Point.Round(b.Centroid), v_center_point);
                            var ang = Math2.angleBetween2Lines(small_line, baseLine);
                            var rotationAngle = Math2.FixAngle(ang, v_center_point, Point.Round(b.Centroid));


                            // CvInvoke.Rectangle(ProcessedFrame, box, new Bgr(Color.White).MCvScalar, 1);

                            var rotated_frame = only_blobs.Rotate(-1 * rotationAngle, new Gray(0));
                            {
                                lastRotatedFrame = rotated_frame.Size;
                                lastRect = new Rectangle(rotated_frame.Width / 2 - 30, 0, 60, 60);

                                var rotated_letter_only = rotated_frame.Copy(lastRect);

                                if (debugState[3] == null)
                                {
                                    debugState[3] = rotated_letter_only;
                                }

                                parts.Add(new CompassPack { Item1 = rotated_letter_only, Item2 = rotationAngle, BlobBox = b.BoundingBox, BlobArea = b.Area });
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex);
                        }
                    }

                    var ret = CompassProcFrame(data.Id, parts, focus, ref debugState);
                    if (!double.IsNaN(ret))
                    {

                        var compass_frame = only_blobs.Rotate(ret, new Gray(0));

                        blobs = new CvBlobs();
                        GetBlobDetector().Detect(compass_frame, blobs);
                        blobs.FilterByArea(25, 250);
                        list_blobs = new List<CvBlob>();

                        var list = blobs.Values.OrderBy(b => b.Centroid.Y);

                        var top = list.First().Centroid;
                        var bottom = list.Last().Centroid;

                        var verticalLine = new LineSegment2DF(new PointF(focus.Size.Width / 2, 0), new PointF(focus.Size.Width / 2, focus.Size.Height));
                        var lineFromNorthToSouth = new LineSegment2DF(top, bottom);
                        var skewAngle = Math2.angleBetween2Lines(lineFromNorthToSouth, verticalLine);
                        skewAngle = (skewAngle * (180 / Math.PI));

                        ret -= skewAngle;

                        compass_frame = only_blobs.Rotate(ret, new Gray(0));

                        var topDotRect = new Rectangle(focusRect.Left - 160, focusRect.Top - 5, 40, 40);
                        var bottomDotRect = new Rectangle(focusRect.Left - 170, focusRect.Bottom - 40, 60, 40);

                        var topPointBlobs = GetDotLocationFromFrame(data.Frame, focusRect, topDotRect, isTop:true);
                        var bottomPointBlobs = GetDotLocationFromFrame(data.Frame, focusRect, bottomDotRect, isTop:false);

                        if (topPointBlobs.Count > 0 && bottomPointBlobs.Count > 0)
                        {
                            var topPoint = topPointBlobs.First().Centroid.Add(topDotRect.Location);
                            var bottomPoint = bottomPointBlobs.First().Centroid.Add(bottomDotRect.Location);

                            var a = Math2.GetPolarHeadingFromLine(topPoint, bottomPoint);

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

                            if (Math.Abs(a) > 8)
                            {
                                // Trace.WriteLine("Rejected due to dots angle out of bounds " + a);
                                return double.NaN;
                            }

                            // Trace.WriteLine($"A:{a}");

                            ret += a / 2;

                            if (ret < 0) ret = 360 - ret;
                            if (ret > 360) ret -= 360;

                            return ret;
                        }


                    }
                }
            }
            return double.NaN;
        }

        private List<CvBlob> GetDotLocationFromFrame(Image<Bgr, byte> frame, Rectangle focusRect, Rectangle dotRect, bool isTop)
        {
            var topDot = frame.SafeCopy(dotRect);
            var topHsv = topDot.Convert<Hsv, byte>();
            var topRange = topHsv.InRange(new Hsv(0, 0, 80), new Hsv(180, 255, 150));


            var blobs = new CvBlobs();
            GetBlobDetector().Detect(topRange, blobs);
            blobs.FilterByArea(5, 18);

            var ret = new List<CvBlob>();
            ret.AddRange(blobs.Values);
            if (isTop)
            {
                return ret.OrderByDescending(b => b.Centroid.Y).ToList();
            }
            else
            {
                return ret.OrderBy(b => b.Centroid.Y).ToList();
            }
        }

        double CompassProcFrame(int frameId, List<CompassPack> packs, Image<Bgr, Byte> compass_frame, ref object[] debugState)
        {

            var my_frameref = Timeline.Data[frameId];
            TimelineFrame last_frameref = null;

            for (var i = frameId - 1; i >= 0; i--)
            {
                var f = Timeline.Data[i];
                if (!double.IsNaN(f.Heading.Value))
                {
                    last_frameref = f;
                    break;
                }
            }


            if (packs != null)
            {
                var choices = new List<Tuple<double, double, string, Image<Gray, byte>, double>>();

                var others = new List<string>();

                double unused_angle = 0;

                var p = packs.OrderByDescending(px => px.Item2);

                var sx = "";
                foreach (var ptx in p) sx += " " + ptx.Item2;

                //  Trace.WriteLine("COMPASS:   " + sx);

                foreach (var pack in packs)
                {
                    var ocr = GetTesseract();
                    ocr.SetImage(pack.Item1);
                    ocr.Recognize();

                    //   pack.Item1.Save($"c:\\save\\{cid++}.png");

                    var small_angle = pack.Item2;
                    var b = pack.BlobBox;

                    float cost = 0;
                    var str = "";
                    foreach (var c in ocr.GetCharacters())
                    {
                        if (!string.IsNullOrWhiteSpace(c.Text)) cost = c.Cost;
                        str += c.Text;
                        str = str.Trim().ToUpper();
                    }

                    str = str.Replace("'", "").Replace(".", "").Replace("‘", "").Replace("‘", "’").Replace("\"", "").Replace(",", "").Replace(":", "");


                    if (str == "VI") str = "W";
                    if (str == "VV") str = "W";
                    if (str == "VY") str = "W";
                    if (str == "XV") str = "W";
                    if (str == "\\N") str = "W";
                    if (str == "5") str = "S";
                    //    if (str == "3") str = "S";
                    if (str == "8") str = "S";
                    if (str == "3") str = "S";
                    if (str == "9") str = "S";
                    if (str == "G") str = "S";
                    //  if (str == "A") str = "S";
                    if (str == "$") str = "S";
                    //   if (str == "Q") str = "S";
                    //  if (str == "B") str = "S";
                    if (str == "U") str = "N"; // WTF lol
                    if (str == "M") str = "N";
                    if (str == "H") str = "N";
                    if (str == "I") str = "N";
                    if (str == "II") str = "N";
                    //    if (str == "LL") str = "N";
                    if (str == "ﬂ") str = "N";
                    if (str == "ﬁ") str = "N";
                    if (str == "F") str = "E";
                    if (str == "É") str = "E";
                    if (str == "=") str = "E";

                    //   Trace.WriteLine("CHO : " + str);


                    if (last_frameref != null &&
                       (my_frameref.Seconds - last_frameref.Seconds) < 1000)
                    {
                        var extended = (CompassExtendedFrame)last_frameref.Heading.ForIndicatorUse;
                        if (str != "N" &&
                            str != "E" &&
                            str != "S" &&
                            str != "W")
                        {

                            var dN = Math.Abs(Math2.DiffAngles(pack.Item2, extended.LastN));
                            var dE = Math.Abs(Math2.DiffAngles(pack.Item2, extended.LastE));
                            var dS = Math.Abs(Math2.DiffAngles(pack.Item2, extended.LastS));
                            var dW = Math.Abs(Math2.DiffAngles(pack.Item2, extended.LastW));

                            //  var diffs = new List<double> { dN, dE, dS, dW };

                            //  Trace.WriteLine(string.Format("dN: {0} dE: {1} dS: {2} dW: {3}", dN, dE, dS, dW));

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
                        }
                    }


                    if (str == "N" ||
                        str == "E" ||
                        str == "S" ||
                        str == "W")
                    {
                        small_angle = 360 - Math.Abs(small_angle);

                        double new_heading = 0;

                        b.Inflate(2, 2);

                        var extended = new CompassExtendedFrame();
                        my_frameref.Heading.ForIndicatorUse = extended;

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

                        //  new_heading -= 1; // 1 degree bias to align with world

                        if (new_heading < 0) new_heading += 360;

                        //   if (str == "W")
                        {

                            choices.Add(new Tuple<double, double, string, Image<Gray, byte>, double>(new_heading, small_angle, str, pack.Item1, pack.BlobArea));
                            //   break;
                        }
                    }
                    else
                    {
                        unused_angle = pack.Item2;

                        others.Add(str);

                    }
                }

                if (choices.Count == 3 && packs.Count == 4)
                {

                    var letters = new List<string>() { "N", "E", "S", "W" };


                    foreach (var c in choices) letters.Remove(c.Item3);


                    var str = letters.First();

                    var o_angle = unused_angle;
                    unused_angle = 360 - Math.Abs(unused_angle);

                    var extended = (CompassExtendedFrame)my_frameref.Heading.ForIndicatorUse;

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

                    //  new_heading -= 1; // 1 degree bias to align with world

                    if (new_heading < 0) new_heading += 360;

                    //     Trace.WriteLine("IMPLICIT: " + str);
                    choices.Add(new Tuple<double, double, string, Image<Gray, byte>, double>(new_heading, (int)unused_angle, str, null, 0));





                }

                if (choices.Count == 4)
                {
                    //  Trace.WriteLine("---------");
                    choices = choices.OrderBy(cx => cx.Item5).ToList();

                    //    LastAction = "Have Choices";

                    // exclude bad combinations
                    if (choices.Where(ct => ct.Item3 == "N").Count() > 1) return double.NaN;
                    if (choices.Where(ct => ct.Item3 == "E").Count() > 1) return double.NaN;
                    if (choices.Where(ct => ct.Item3 == "S").Count() > 1) return double.NaN;
                    if (choices.Where(ct => ct.Item3 == "W").Count() > 1) return double.NaN;

                    var p1 = Math2.AddAngles(choices[0].Item1, choices[1].Item1);
                    var p2 = Math2.AddAngles(choices[2].Item1, choices[3].Item1);
                    var nextHeading = Math2.AddAngles(p1, p2);

                    //var nextHeading = choices.First(ct => ct.Item3 == "S").Item1;

                    return nextHeading;



                }
                else
                {
                    var ret = "";

                    foreach (var r in choices) ret += r.Item3 + " ";

                    ret += "| OTHER: |   ";

                    var o_ret = "";
                    foreach (var o in others) o_ret += o + " ";

                    if (!string.IsNullOrWhiteSpace(o_ret))
                    {
                        ret += o_ret;
                        //  Trace.WriteLine("CC: " + ret);
                    }
                    //  LastAction = "No choices " + ret;
                }
            }
            return double.NaN;
        }


        class CompassPack
        {
            public Image<Gray, byte> Item1;
            public double Item2;
            public Rectangle BlobBox;
            public double BlobArea;
        }

        // TODO: factor this all out somewhere
        ConcurrentDictionary<int, CvBlobDetector> BlobDetectors = new ConcurrentDictionary<int, CvBlobDetector>();


        CvBlobDetector CrateBlobDetector()
        {
            return new CvBlobDetector();
        }

        protected CvBlobDetector GetBlobDetector()
        {
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

            if (!BlobDetectors.Keys.Contains(tid))
            {
                BlobDetectors.TryAdd(tid, CrateBlobDetector());
            }
            return BlobDetectors[tid];
        }

        // TODO also remove

        ConcurrentDictionary<int, Tesseract> Tessreacts = new ConcurrentDictionary<int, Tesseract>();

        Tesseract CreateTesseract()
        {
            var ocr = new Tesseract();
            ocr.Init("", "eng", OcrEngineMode.TesseractOnly);
            return ocr;
        }

        protected Tesseract GetTesseract()
        {
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

            if (!Tessreacts.Keys.Contains(tid))
            {
                Tessreacts.TryAdd(tid, CreateTesseract());
            }
            return Tessreacts[tid];
        }


    }
}
