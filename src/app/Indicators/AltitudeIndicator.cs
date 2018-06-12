using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using Emgu.CV.Cvb;
using System.Diagnostics;
using GTAPilot;

public class AltitudeIndicator : Indicator
{
    protected override InputType Type { get { return InputType.Altitude; } }

    protected override void OnSuccessFrameCore(IndicatorData data, ref double ObservedValue)
    {


           // SetpointValues.Add(new IndicatorValueData { Tick = _panel.Ticks, Value = _panel.Computer.DesiredAltitude });

        
    }

    public AltitudeIndicator()
    {
        TuningValues.Add(new Hsv(0, 0, 120));
        //  TuningValues.Add(new Hsv(0, 0, 70));
        TuningValues.Add(new Hsv(0, 0, 80));
      //  TuningValues.Add(new Hsv(0, 0, 90));
      //  TuningValues.Add(new Hsv(0, 0, 100));
      ////  TuningValues.Add(new Hsv(0, 0, 110));
    //    
       // TuningValues.Add(new Hsv(0, 0, 130));
      //  TuningValues.Add(new Hsv(0, 0, 140));
    }

    int LastBig = 0;

    protected override Rectangle FastROI
    {
        get
        {
            if (Hints.AttitudeIndicator.Center.X > 0)
            {
                var r = (int)Hints.PitchIndicator.Radius;
                var d = (int)Hints.PitchIndicator.Radius * 2;
                return (new Rectangle((int)Hints.PitchIndicator.Center.X + r + Hints.PitchRect.Left, (int)Hints.PitchIndicator.Center.Y - r + Hints.PitchRect.Top - 220, 150, 150));
            }
            else
            {
                return default(Rectangle);
            }
        }
    }

    double last_angle = double.NaN;

    protected override bool ProcessFrameCore(IndicatorData data, ref double ObservedValue)
    {
        // cab skew
       // ProcessedFrame = ProcessedFrame.Rotate(0, new Bgr(Color.Black));

        var vs_hsv = data.Frame.Convert<Hsv, byte>();
        var black_1 = vs_hsv[2]; //.InRange(new Hsv(0, 0, 0), new Hsv(180, 255, 50));

        // cab skew


        var big_circles = black_1.Convert<Bgr, byte>();

   //     black_1 = black_1.PyrUp().PyrDown();

        //  var circles = CvInvoke.HoughCircles(black_1, HoughType.Gradient, 2.0, 2, 4, 10, 40, 80); // 20, 10, 180, 30, 150);
        var circles = CvInvoke.HoughCircles(black_1, HoughType.Gradient, 2.0, 80, 4, 50, 40, 50); // 20, 10, 180, 30, 150);
        if (circles.Length == 0)
        {
          //  circles = CvInvoke.HoughCircles(black_1, HoughType.Gradient, 2.0, 10, 4, 80, 40, 80); // 20, 10, 180, 30, 150);

        }

        foreach (var c in circles)
        {
            CvInvoke.Circle(big_circles, Point.Round(c.Center), (int)c.Radius, new Bgr(Color.Red).MCvScalar, 1);
        }

        if (circles.Length == 1)
        {
            var airspeed_circle = circles[0];
            var best_circle = airspeed_circle;

          //  Trace.WriteLine("CIRCCCCC: " + best_circle.Radius);

            var margin = 0;
            var d = (int)airspeed_circle.Radius * 2;
            var r = (int)airspeed_circle.Radius;
            var new_rect = new Rectangle((int)airspeed_circle.Center.X - r - margin, (int)airspeed_circle.Center.Y - r - margin, d + margin * 2, d + margin * 2);
            var altitudeFrame = data.Frame.Copy(new_rect);


            vs_hsv = altitudeFrame.Convert<Hsv, byte>();

            // NIGHT/OTHER:
            // var vs_blackimg = vs_hsv.InRange(new Hsv(0, 0, 120), new Hsv(180, 255, 255));
            // DAYLIGHT:
            var vs_blackimg =  vs_hsv.InRange(TuningValue, new Hsv(180, 255, 255));

            

            Mat vspeedMask = new Mat(vs_blackimg.Size, DepthType.Cv8U, 3);
            vspeedMask.SetTo(new MCvScalar(1));
            CvInvoke.Circle(vspeedMask, Point.Round(new PointF(r + margin, r + margin)), (int)(r - (r * 0)), new Bgr(Color.White).MCvScalar, -1);

            vs_blackimg = vs_blackimg.Copy(vspeedMask.ToImage<Gray, byte>());

            var markedup_frame = vs_blackimg.Convert<Bgr, byte>();

            IntermediateFrameBgr = markedup_frame;

            // IntermediateFrameGray = vs_blackimg;

            var cannyEdges3 = new Mat();
            CvInvoke.Canny(vs_blackimg, cannyEdges3, 10, 40);
            var lines = CvInvoke.HoughLinesP(
               cannyEdges3,
               1,Math.PI / 45.0, 12, 24, 3);

            lines = lines.OrderByDescending(l => l.Length).ToArray();

            foreach (var line in lines)
            {

                CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);
            }

            foreach (var line in lines)
            {

              //  CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);

                var center_size = 25;
                var center_point = new Point((altitudeFrame.Width / 2) - 4, (altitudeFrame.Height / 2) + 4);
                var center_box_point = new Point((altitudeFrame.Width / 2) - (center_size / 2), 4 + (altitudeFrame.Height / 2) - (center_size / 2));
                Rectangle center = new Rectangle(center_box_point, new Size(center_size, center_size));

                CvInvoke.Rectangle(markedup_frame, center, new Bgr(Color.Red).MCvScalar, 1);

                if (center.Contains(line.P1) || center.Contains(line.P2))
                {
                    Point other_point;
                    Point real_center_point;
                    if (center.Contains(line.P1))
                    {
                        other_point = line.P2;
                        real_center_point = line.P1;
                    }
                    else
                    {
                        other_point = line.P1;
                        real_center_point = line.P2;
                    }

                    LineSegment2D baseLine = new LineSegment2D(
                        new Point((altitudeFrame.Width / 2), 0), new Point((altitudeFrame.Width / 2), (altitudeFrame.Height / 2))

                        );

 

                    /*
                    var center_full = new Point(center_point.X + base_rect.X, center_point.Y + base_rect.Y);
                    var other_full = new Point(other_point.X + base_rect.X, other_point.Y + base_rect.Y);

                    CvInvoke.Line(data.Frame, center_full, other_full, new Bgr(Color.Black).MCvScalar, 3);
                    CvInvoke.Line(data.Frame, center_full, other_full, new Bgr(Color.Lime).MCvScalar, 1);
                    */



                  //  CvInvoke.Line(ProcessedFrame, center_point, other_point, new Bgr(Color.Yellow).MCvScalar, 1);

                    //   CvInvoke.Line(ProcessedFrame, real_center_point, other_point, new Bgr(Color.Red).MCvScalar, 1);

                    Mat without_small_line_mask = new Mat(vs_blackimg.Size, DepthType.Cv8U, 3);
                    without_small_line_mask.SetTo(new Bgr(Color.White).MCvScalar);
                //    CvInvoke.Line(without_small_line_mask, center_point, other_point, new Bgr(Color.Black).MCvScalar, 3);
                    CvInvoke.Line(without_small_line_mask, real_center_point, other_point, new Bgr(Color.Black).MCvScalar, 4);

                    var masked_line = vs_blackimg.Copy(without_small_line_mask.ToImage<Gray, byte>());

                  //  IntermediateFrameGray = masked_line;
                  //  return false;

                    CvBlobs blobs = new CvBlobs();

                    GetBlobDetector().Detect(masked_line, blobs);
                    blobs.FilterByArea(50, 2000);

                    foreach (var pair in blobs)
                    {
                        //   Trace.WriteLine("BLOB: " + pair.Value.Area);

                         CvBlob b = pair.Value;
                         CvInvoke.Rectangle(markedup_frame, b.BoundingBox, new Bgr(Color.White).MCvScalar, 1);
                        // CvInvoke.Circle(ProcessedFrame, Point.Round(b.Centroid), 2, new Bgr(Color.LimeGreen).MCvScalar, -1);
                    }

                    bool big_set = false;
                    Point big_center = new Point();
                    Point big_other = new Point();

                    if (blobs.Count > 0)
                    {
                        var top_blob = blobs.OrderBy(x => x.Value.BoundingBox.Width > x.Value.BoundingBox.Height ? x.Value.BoundingBox.Width : x.Value.BoundingBox.Height).FirstOrDefault();
                        // CvInvoke.Rectangle(ProcessedFrame, top_blob.Value.BoundingBox, new Bgr(Color.LimeGreen).MCvScalar, 1);
                        // CvInvoke.Circle(ProcessedFrame, Point.Round(top_blob.Value.Centroid), 2, new Bgr(Color.LimeGreen).MCvScalar, -1);

                        big_center = top_blob.Value.GetContour().OrderByDescending(x => Math2.GetDistance(center_point, x)).First();
                        big_other = top_blob.Value.GetContour().OrderBy(x => Math2.GetDistance(center_point, x)).First();

                        var dist = Math2.GetDistance(big_other, center_point);
                       // Trace.WriteLine("DIST: " + dist);

                        big_set = dist < 15;

                        if (big_set)
                        {
                          //  Trace.WriteLine("BI: " + Math2.GetDistance(big_center, big_other));
                            big_set = Math2.GetDistance(big_center, big_other) < 25;
                        }
                        //  big_center = Point.Round(top_blob.Value.Centroid);
                        IntermediateFrameBgr = markedup_frame;


                    }
                    /*
                    else
                    {
                        LastAction = "No big blobs";
                        //imageBox10.Image = vs_blackimg;
                        //   Trace.WriteLine("coudln't set BIG");
                        return false;
                    }
                    */

                    center_point = real_center_point;

                    if (big_set)
                    {
                       // CvInvoke.Line(altitudeFrame, big_other, big_center, new Bgr(Color.Blue).MCvScalar, 2);
                        CvInvoke.Line(markedup_frame, big_other, big_center, new Bgr(Color.Blue).MCvScalar, 2);

                     //   var big_other_full = new Point(big_other.X + base_rect.X, big_other.Y + base_rect.Y);
                     //   var big_center_full = new Point(big_center.X + base_rect.X, big_center.Y + base_rect.Y);

                     //   CvInvoke.Line(data.Frame, big_other_full, big_center_full, new Bgr(Color.Black).MCvScalar, 3);
                     //   CvInvoke.Line(data.Frame, big_other_full, big_center_full, new Bgr(Color.Blue).MCvScalar, 1);


                    }
                    /*
                    if (big_set)
                    {
                        LineSegment2D big_line = new LineSegment2D(big_center, center_point);



                        //    var small_angle = Math2.FixAngle(Math2.angleBetween2Lines(small_line, baseLine), center_point, other_point);
                        var big_angle = Math2.FixAngle(Math2.angleBetween2Lines(big_line, baseLine), center_point, big_center);

                        LastBig = Math.Round((big_angle / 360) * 10, 2);

                        // world can't go higher than 8.5
                        // use 6.5 to avoid the text there though
                        if (LastBig >=6.5) LastBig = 0;


                        var far_points_dist = Math2.GetDistance(other_point, big_other);

                     //   if (far_points_dist < 20)
                     //       return false;


                      //  Trace.WriteLine("##### BIG: " + LastBig);
                    }
                    */

                 //   var dist2 = Math2.GetDistance(other_point, center_point);

                   // if (dist2 < 25) return false;

                  //  Trace.WriteLine("SMALL DIST: " + dist2);

                    // imageBox2.Image = masked_line;
                    LineSegment2D small_line = new LineSegment2D(other_point, center_point);

                    var small_angle = Math2.FixAngle(Math2.angleBetween2Lines(small_line, baseLine), center_point, other_point);


                    var hundreds = Math.Round((small_angle / 360) * 1000);

                    CvInvoke.Line(markedup_frame, center_point, other_point, new Bgr(Color.Purple).MCvScalar, 1);

                    // ground
                    //  if (small_angle < 30 && LastBig == 0) hundreds = 0;


                    //  small_angle = Math.Round((small_angle / 360) * 10);
                    IntermediateFrameBgr = markedup_frame;



                    /*
                    int the_big = 0;
                    if (hundreds >= 5) the_big = (int)Math.Floor(LastBig);
                    else the_big = (int)Math.Ceiling(LastBig);
                    */
                    //  ObservedValue = the_big;
                    /*
                    int the_big = -1;
                    if (hundreds >= 0 && hundreds <= 300)
                    {
                        the_big = (int)Math.Round(LastBig);
                    }
                    else if (hundreds > 300 && hundreds <= 500)
                    {
                        the_big = (int)Math.Floor(LastBig);
                    }
                    else if (hundreds > 500 && hundreds <= 700)
                    {
                        the_big = (int)Math.Ceiling(LastBig) - 1;
                        if (the_big < 0) the_big = 0;
                    }
                    else if (hundreds > 700 && hundreds < 1000)
                    {
                        the_big = (int)Math.Round(LastBig) - 1;
                        if (the_big < 0) the_big = 0;
                    }
                    else
                    {
                        hundreds = 0;
                        the_big = (int)Math.Round(LastBig);
                        // 10
                    }



                    //   int the_big = (int)Math.Floor(LastBig);


                    */

                    if (hundreds == 1000) hundreds = 0;


                    Func<int, double> GetNextValue = (int big) =>
                    {
                        if (big == -1) return -1000;

                        double val = 0;
                        if (hundreds < 100)
                        {
                            val = double.Parse(big + "0" + hundreds); // double.Parse(hundreds);
                        }
                        else
                        {
                            val = double.Parse(big + "" + hundreds); // double.Parse(hundreds);
                        }
                        return val;
                    };

                    double nextValue = GetNextValue(LastBig);


                    var max_change = 400;

                    if (Math.Abs(nextValue - LastGoodValueAverage) > max_change)
                    {
                        if (num_rejected_values < 50)
                        {



                            var increasedNextValue = GetNextValue(LastBig + 1);
                            var decresedNextValue = GetNextValue(LastBig - 1);
                            if (Math.Abs(increasedNextValue - last_value) < max_change)
                            {
                                LastBig++;
                                nextValue = increasedNextValue;
                            }
                            else if (Math.Abs(decresedNextValue - last_value) < max_change)
                            {
                                LastBig--;
                                nextValue = decresedNextValue;
                            }
                            else
                            {
                                num_rejected_values++;
                             //   Trace.WriteLine("rejected altitude value: " + nextValue + " " + increasedNextValue + " " + decresedNextValue);
                                // ????
                                return false;
                            }

                        }
                        else
                        {
                            if (big_set)
                            {
                            //    Trace.WriteLine("ALT RESET");

                                LineSegment2D big_line = new LineSegment2D(big_center, center_point);



                                //    var small_angle = Math2.FixAngle(Math2.angleBetween2Lines(small_line, baseLine), center_point, other_point);
                                var big_angle = Math2.FixAngle(Math2.angleBetween2Lines(big_line, baseLine), center_point, big_center);

                                LastBig = (int)Math.Round((big_angle / 360) * 10, 2);
                                ObservedValue = GetNextValue(LastBig);
                            }
                        }
                    }

                    num_rejected_values = 0;


                    if (nextValue < 35) return false;

                    if (!double.IsNaN(last_angle))
                    {
                        var a = small_angle;
                        var b = last_angle;
                        var diff = ((a - b + 180 + 360) % 360) - 180;

                        //Trace.WriteLine("ALT diff: " + diff);

                        if (Math.Abs(diff) > 45)
                        {
                            num_rejected_values++;
                               return false;
                        }
                    }

                    last_angle = small_angle;

                    ObservedValue = nextValue;


                    /*
                    if (change > 300 && num_rejected_values < 10)
                    {
                        num_rejected_values++;
                       // Trace.WriteLine("Rejected altitude change due to size: " + change + " (stay at " + last_value + ")");
                        return false;
                    }

               //     reset:
               */
                    last_value = ObservedValue;
                    num_rejected_values = 0;

                  //  Trace.WriteLine("ALT: " + ObservedValue + " " + LastBig + " " + hundreds);


                    return true;


                }
            }

            IntermediateFrameBgr = markedup_frame;


            if (lines.Length == 0) LastAction = "No lines";
         //   IntermediateFrameGray = vs_blackimg;
        }
        else
        {
            IntermediateFrameBgr = big_circles;
            LastAction = "Didn't find main circle.";
        }

        return false;
    }

    double last_value = 0;
    int num_rejected_values = 0;
}