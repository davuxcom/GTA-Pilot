using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using GTAPilot;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

public class CompassIndicator : Indicator
{
    protected override InputType Type { get { return InputType.Yaw; } }

    public CompassIndicator()
    {
      //  TuningValues.Add(new Hsv(0, 0, 130));
        TuningValues.Add(new Hsv(0, 0, 140));
      //  TuningValues.Add(new Hsv(0, 0, 110));
    //    TuningValues.Add(new Hsv(0, 0, 100));
        TuningValues.Add(new Hsv(0, 0, 90));
       // TuningValues.Add(new Hsv(0, 0, 80));
       // TuningValues.Add(new Hsv(0, 0, 70));


        // 100: daylight
        // 120: night
    }

    protected override void OnSuccessFrameCore(IndicatorData data, ref double ObservedValue)
    {
        /*
        var is_ok_gps_time = _panel.GPS.LastTime_GPS <= DateTime.Now;

        if (is_ok_gps_time)
        {
            _panel.GPS.Heading = ObservedValue;
            _panel.GPS.Speed = _panel.Speed.LastGoodValueAverage;
            _panel.GPS.Tick(DateTime.Now);
        }
        */
    }

    protected override double AverageCore(double[] values)
    {
        if (values.Length == 0) return double.NaN;
        else if (values.Length == 1) return values[0];
        else
        {
            var a = Math2.AddAngles(values[0], values[1]);

            for (var i = 2; i < values.Length; ++i)
            {
                a = Math2.AddAngles(values[i], a);
            }
            return a;
        }
    }

    class CompassPack
    {
        public Image<Gray, byte> Item1;
        public double Item2;
        public Rectangle BlobBox;
        public double BlobArea;
    }

    protected override Rectangle FastROI
    {
        get
        {
            if (Hints.AttitudeIndicator.Center.X > 0)
            {
                var r = (int)Hints.AttitudeIndicator.Radius;
                var d = (int)Hints.AttitudeIndicator.Radius * 2;
                return (new Rectangle((int)Hints.AttitudeIndicator.Center.X + (int)(d*2.4) + Hints.PitchRect.Left, (int)Hints.PitchIndicator.Center.Y - d - d + Hints.PitchRect.Top, 190, 190));
            }
            else
            {
                return default(Rectangle);
            }
        }
    }

    double CompassProcFrame(IndicatorData data, List<CompassPack> packs, Image<Bgr, Byte> compass_frame)
    {
        
        var my_frameref = Timeline.Data[data.Id];

        TimelineFrame last_frameref = null;

        for (var i = data.Id; i >= 0; i--)
        {
            var f = Timeline.Data[i];
            if (!double.IsNaN(f.Heading))
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
                ocr.Recognize(pack.Item1);

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

                //  Trace.WriteLine("CHO : " + choices.Count);

                if (last_frameref != null &&
                   (my_frameref.Time - last_frameref.Time).TotalMilliseconds < 1000)
                {

                    if (str != "N" &&
                        str != "E" &&
                        str != "S" &&
                        str != "W")
                    {

                        var dN = Math.Abs(Math2.DiffAngles(pack.Item2, last_frameref.Extended.LastN));
                        var dE = Math.Abs(Math2.DiffAngles(pack.Item2, last_frameref.Extended.LastE));
                        var dS = Math.Abs(Math2.DiffAngles(pack.Item2, last_frameref.Extended.LastS));
                        var dW = Math.Abs(Math2.DiffAngles(pack.Item2, last_frameref.Extended.LastW));

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

                    switch (str)
                    {
                        case "N":
                            my_frameref.Extended.LastN = pack.Item2;
                            new_heading = small_angle;
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Blue).MCvScalar, 1);

                            break;
                        case "E":
                            my_frameref.Extended.LastE = pack.Item2;
                            new_heading = (small_angle + 90);
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Yellow).MCvScalar, 1);

                            break;
                        case "S":
                            my_frameref.Extended.LastS = pack.Item2;
                            new_heading = (small_angle + 180);
                            CvInvoke.Rectangle(compass_frame, b, new Bgr(Color.Red).MCvScalar, 1);

                            break;
                        case "W":
                            my_frameref.Extended.LastW = pack.Item2;
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

                double new_heading = 0;
                switch (str)
                {
                    case "N":
                        my_frameref.Extended.LastN = o_angle;
                        new_heading = unused_angle;

                        break;
                    case "E":
                        my_frameref.Extended.LastE = o_angle;

                        new_heading = (unused_angle + 90);

                        break;
                    case "S":
                        my_frameref.Extended.LastS = o_angle;

                        new_heading = (unused_angle + 180);

                        break;
                    case "W":
                        my_frameref.Extended.LastW = o_angle;

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

                LastAction = "Have Choices";

                // exclude bad combinations
                if (choices.Where(ct => ct.Item3 == "N").Count() > 1) return double.NaN;
                if (choices.Where(ct => ct.Item3 == "E").Count() > 1) return double.NaN;
                if (choices.Where(ct => ct.Item3 == "S").Count() > 1) return double.NaN;
                if (choices.Where(ct => ct.Item3 == "W").Count() > 1) return double.NaN;

                var p1 = Math2.AddAngles(choices[0].Item1, choices[1].Item1);
                var p2 = Math2.AddAngles(choices[2].Item1, choices[3].Item1);
                var nextHeading = Math2.AddAngles(p1, p2);

                var copy_frame = compass_frame; //.Copy();
                
                    compass_frame = copy_frame.Rotate(360 - nextHeading, new Bgr(Color.Black));


                    


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
                LastAction = "No choices " + ret;
            }
        }
        return double.NaN;
    }

    int num_rejected_values = 0;


    Image<Bgr, byte> CropCompass(IndicatorData data)
    {
        var compass_gray_frame = new Mat();
        CvInvoke.CvtColor(data.Frame, compass_gray_frame, ColorConversion.Bgr2Gray);

        var pyrDown = new Mat();
        CvInvoke.PyrDown(compass_gray_frame, pyrDown);
        CvInvoke.PyrUp(pyrDown, compass_gray_frame);
        var circles = CvInvoke.HoughCircles(compass_gray_frame, HoughType.Gradient, 2.0, 30, 10, 120, 35, 65);
        if (circles.Length > 0)
        {
            var margin = 6;
            var circle = circles[0];
            var d = (int)circle.Radius * 2;
            var r = (int)circle.Radius;
            var new_rect = new Rectangle((int)circle.Center.X - r - margin, (int)circle.Center.Y - r - margin, d + margin * 2, d + margin * 2);

            return data.Frame.Copy(new_rect);
        }
        return null;
    }

    double GetBiasAngleViaTriangle(Image<Bgr, byte> compassFrame)
    {
        var vs_hsv = compassFrame.Convert<Hsv, byte>();

        //  var triangle = vs_hsv.InRange(HsvTuner.Low, HsvTuner.High);
        var triangle = vs_hsv.InRange(new Hsv(170, 100, TuningValue.Value - 10), new Hsv(180, 255, 255));

        CvBlobs small_blobs = new CvBlobs();
        GetBlobDetector().Detect(triangle, small_blobs);
        small_blobs.FilterByArea(1, 10);

        Mat maskBlobs = new Mat(triangle.Size, DepthType.Cv8U, 3);

        maskBlobs.SetTo(new Bgr(Color.White).MCvScalar);

        foreach (var b in small_blobs)
        {
            CvInvoke.Rectangle(maskBlobs, b.Value.BoundingBox, new Bgr(Color.Black).MCvScalar, -1);
        }

        var blobImage = maskBlobs.ToImage<Gray, byte>();

        triangle = triangle.Copy(blobImage);

        // IntermediateFrameGray = triangle;

        var edges = new Mat();

        var lines = CvInvoke.HoughLinesP(triangle, 1, Math.PI / 45.0, 1, 1, 5).ToList();

        if (lines.Count > 0)
        {
            var markedup_triangle = triangle.Convert<Bgr, byte>();

            //  var lx = "";

            foreach (var l in lines)
            {
                CvInvoke.Line(markedup_triangle, l.P1, l.P2, new Bgr(Color.Red).MCvScalar, 1);
                //  lx += Math2.GetDistance(l.P1, l.P2) + " ";
            }

            CvInvoke.Line(markedup_triangle,
                new Point(markedup_triangle.Width / 2, 0),
                new Point(markedup_triangle.Width / 2, markedup_triangle.Height),
                new Bgr(Color.Lime).MCvScalar, 1);


            var right_bucket = lines.Where(px => px.P1.X > triangle.Width / 2);
            var left_bucket = lines.Where(px => px.P1.X < triangle.Width / 2);

            var left_line = left_bucket.OrderByDescending(p => p.P1.Y > p.P2.Y ? p.P1.Y : p.P2.Y).First();
            var right_line = right_bucket.OrderByDescending(p => p.P1.Y > p.P2.Y ? p.P1.Y : p.P2.Y).First();

            var left_point = left_line.P1.Y > left_line.P2.Y ? left_line.P1 : left_line.P2;
            var right_point = right_line.P1.Y > right_line.P2.Y ? right_line.P1 : right_line.P2;


            CvInvoke.Line(markedup_triangle,
                left_point,
                right_point,
                new Bgr(Color.Yellow).MCvScalar, 1);

            LineSegment2D baseLine = new LineSegment2D(new Point((triangle.Width / 2), 0), new Point((triangle.Width / 2), (triangle.Height / 2)));
            LineSegment2D small_line = new LineSegment2D(left_point, right_point);

            var small_angle = Math2.FixAngle(Math2.angleBetween2Lines(small_line, baseLine), right_point, left_point);
            small_angle -= 270;

            if (Math.Abs(small_angle) < 10)
            {
                return small_angle;
            }
        }
        return 0;
    }

    protected override bool ProcessFrameCore(IndicatorData data, ref double ObservedValue)
    {
        
        double NextObservedValue = double.NaN;
        LastAction = "Bad ROI";
        if (!data.IsFastROI) return false;
        LastAction = "Start";

        double BIAS_ANGLE = 0;

        var compassFrame = CropCompass(data);
        if (compassFrame != null)
        {
            LastAction = "Getting Bias";
            BIAS_ANGLE = GetBiasAngleViaTriangle(compassFrame);
           // Trace.WriteLine("COMPASS BIAS: " + BIAS_ANGLE);
            LastAction = "OK Bias";
            var vs_hsv = compassFrame.PyrUp().PyrDown().Convert<Hsv, byte>();

            var vs_text = vs_hsv.InRange(TuningValue, new Hsv(180, 255, 255));
            var vs_mask = vs_hsv.InRange(new Hsv(0, 0, 0), new Hsv(180, 140, 255));
            var vs_textonly = vs_text.Copy(vs_mask);
            var markedup_textonly = vs_textonly.Convert<Bgr, byte>();


            CvBlobs blobs = new CvBlobs();
            GetBlobDetector().Detect(vs_textonly, blobs);
            blobs.FilterByArea(25, 250);
            Mat blobMask = new Mat(vs_hsv.Size, DepthType.Cv8U, 3);
            blobMask.SetTo(new MCvScalar(1));

            // Select 4 largest
            var list_blobs = new List<CvBlob>();
            foreach (var b in blobs) list_blobs.Add(b.Value);
            list_blobs = list_blobs.OrderByDescending(bx => bx.Area).Take(Math.Min(4, blobs.Count)).ToList();

            for (int i_blob = 0; i_blob < list_blobs.Count; i_blob++)
            {
                CvBlob b = list_blobs[i_blob];
                CvInvoke.Rectangle(blobMask, b.BoundingBox, new Bgr(Color.White).MCvScalar, -1);
                CvInvoke.Rectangle(markedup_textonly, b.BoundingBox, new Bgr(Color.Red).MCvScalar, -1);
            }

            IntermediateFrameBgr = markedup_textonly;

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
                LastAction = "Checking Results";
                if (results.Count != 2) return false;
                LastAction = "Have Results";

            }
            else
            {
                return false; // no blobs
            }

            var parts = new List<CompassPack>();
            var only_blobs = vs_textonly.Copy(blobMask.ToImage<Gray, byte>());

            Point v_center_point = new Point(compassFrame.Width / 2, compassFrame.Height / 2);
            Rectangle lastRect = default(Rectangle);
            Size lastRotatedFrame = default(Size);


            LineSegment2D baseLine = new LineSegment2D(new Point((compassFrame.Width / 2), 0), new Point((compassFrame.Width / 2), (compassFrame.Height / 2)));

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
                    var small_angle = Math2.FixAngle(ang, v_center_point, Point.Round(b.Centroid));
                    var biased_angle = small_angle;
                    var angle = small_angle;



                    // CvInvoke.Rectangle(ProcessedFrame, box, new Bgr(Color.White).MCvScalar, 1);


                    var rotated_frame = only_blobs.Rotate(-1 * angle, new Gray(0));
                    {
                        lastRotatedFrame = rotated_frame.Size;
                        lastRect = new Rectangle(rotated_frame.Width / 2 - 15 - 4, 0, 50, 30);

                        var rotated_letter_only = rotated_frame.Copy(lastRect);
                        parts.Add(new CompassPack { Item1 = rotated_letter_only, Item2 = biased_angle, BlobBox = b.BoundingBox, BlobArea = b.Area });
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }
            NextObservedValue = CompassProcFrame(data, parts, compassFrame);
        }

        if (!double.IsNaN(NextObservedValue))
        {
            LastAction = "Have NextValue";


            // a/3 -> 0.5
            // a/2 -> 1
            // a -> 2.35
            // a*1.5 -> 2.75

            var bias = (BIAS_ANGLE / 3) + 0.5;

            ObservedValue = NextObservedValue + bias; // + 1; // + 2.75;

            var my_frameref = Timeline.Data[data.Id];


            my_frameref.Extended.Bias = bias;

            while (ObservedValue < 0) ObservedValue = 360 - ObservedValue;
            while (ObservedValue > 360) ObservedValue -= 360;

            
            TimelineFrame last_frameref = null;

            for (var i = data.Id; i >= 0; i--)
            {
                var f = Timeline.Data[i];
                if (!double.IsNaN(f.Heading))
                {
                    last_frameref = f;
                    break;
                }
            }
            



            if (last_frameref != null &&
                (data.Timestamp - last_frameref.Time).TotalMilliseconds < 500 &&
                Math2.DiffAngles(ObservedValue, last_frameref.Heading) > 60 && num_rejected_values < 20)
            {
                num_rejected_values++;
                return false;
            }

            num_rejected_values = 0;

            return true;
        }

        return false;
    
    }
}