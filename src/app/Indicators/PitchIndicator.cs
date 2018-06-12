using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class PitchIndicator : Indicator
{
    protected override InputType Type { get { return InputType.Pitch; } }

    public PitchIndicator()
    {
       // TuningValues.Add(new Hsv(20, 0, 30));
       // TuningValues.Add(new Hsv(20, 0, 60));


        TuningValues.Add(new Hsv(0, 0, 80));
        TuningValues.Add(new Hsv(0, 0, 90));
        TuningValues.Add(new Hsv(0, 0, 100));
        TuningValues.Add(new Hsv(0, 0, 110));
        TuningValues.Add(new Hsv(0, 0, 120));
        TuningValues.Add(new Hsv(0, 0, 130));
    }

    protected override Rectangle FastROI
    {
        get
        {
            if (Hints.AttitudeIndicator.Center.X > 0)
            {
                var margin = 40;
                var r = (int)Hints.AttitudeIndicator.Radius;
                var d = (int)Hints.AttitudeIndicator.Radius * 2;

                return new Rectangle((int)Hints.AttitudeIndicator.Center.X - r - margin, (int)Hints.AttitudeIndicator.Center.Y - r - margin + d + 40, d + margin * 2, d + margin * 2);

            }
            else
            {
                return default(Rectangle);
            }
        }
    }

    protected override bool ProcessFrameCore(IndicatorData data, ref double ObservedValue)
    {
        if (!data.IsFastROI) return false;
        /*
        if (Hints.AttitudeIndicator.Center.X == 0) return false;

        var margin = 40;
        var r = (int)Hints.AttitudeIndicator.Radius;
        var d = (int)Hints.AttitudeIndicator.Radius * 2;

        var vspeed_rect = new Rectangle((int)Hints.AttitudeIndicator.Center.X - r - margin, (int)Hints.AttitudeIndicator.Center.Y - r - margin + d + 40, d + margin * 2, d + margin * 2);
        ProcessedFrame = data.Frame.Copy(vspeed_rect);

        var base_rect = vspeed_rect;
        */
     //   IntermediateFrameBgr = ProcessedFrame;
     

        LastAction = "after copy";

        Hints.PitchRect = data.ROI; // TODO refactor lol

        //imageBox4.Image = vspeed;
        var vs_hsv = data.Frame.Convert<Hsv, byte>();

      //  var markedup_circles = vs_hsv[2].Convert<Bgr, byte>();

        var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 80, 10, 180, 30, 100);
        if (circles.Length == 0)
        {
            circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 100, 10, 80, 50, 100);


        }

     //   foreach (var c in circles)
        {
      //      CvInvoke.Circle(markedup_circles, Point.Round(c.Center), (int)c.Radius, new Bgr(Color.Red).MCvScalar, 1);
        }

        LastAction = "after CIRC";

        if (circles.Length == 1)
        {
            var best_circle = circles[0];

           // var fixed_circle = new CircleF(new PointF(best_circle.Center.X + data.ROI.X, best_circle.Center.Y + data.ROI.Y), best_circle.Radius);


            Hints.PitchIndicator = best_circle;
            int margin = 10;

            int d = (int)best_circle.Radius * 2;
            int r = (int)best_circle.Radius;

            var new_rect = new Rectangle((int)best_circle.Center.X - r - margin, (int)best_circle.Center.Y - r - margin, d + margin * 2, d + margin * 2);
            var pitchFrame = data.Frame.Copy(new_rect);

            //   base_rect.X += new_rect.X;
            //  base_rect.Y += new_rect.Y;

            // cab skew
          //  pitchFrame = pitchFrame.Rotate(1, new Bgr(Color.Black));

            vs_hsv = pitchFrame.Convert<Hsv, byte>();


            // NIGHT/OTHER:
            //  var vs_blackimg = vs_hsv.InRange(new Hsv(0, 0, 120), new Hsv(180, 255, 255));
            // DAYLIGHT:
            var vs_blackimg = vs_hsv.InRange(TuningValue, new Hsv(180, 255, 255));

            var vspeedMask = new Mat(pitchFrame.Size, DepthType.Cv8U, 3);
            vspeedMask.SetTo(new MCvScalar(1));
            CvInvoke.Circle(vspeedMask, Point.Round(new PointF(circles[0].Radius + margin, circles[0].Radius + margin)), (int)(circles[0].Radius - (circles[0].Radius * 0.1)), new Bgr(Color.White).MCvScalar, -1);

            vs_blackimg = vs_blackimg.Copy(vspeedMask.ToImage<Gray, byte>());
            var vspeed_inner_only = vs_blackimg.Copy(new Rectangle(0, 0, vs_blackimg.Width / 2, vs_blackimg.Height));

            var markedup_frame = vs_blackimg.Convert<Bgr, byte>();

            
            var cannyEdges3 = new Mat();
            CvInvoke.Canny(vspeed_inner_only, cannyEdges3, 10, 140);
            IntermediateFrameGray = cannyEdges3.ToImage<Gray, byte>();

            LineSegment2D[] lines = CvInvoke.HoughLinesP(
               vspeed_inner_only,
               1, //Distance resolution in pixel-related units
               Math.PI / 45.0, 20, 20, 14);

            foreach (LineSegment2D line in lines)
            {

                CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);


             //   var best_speed_line = lines.Take(1).First();
              //  var line = lines.Take(1).First();

                var center_size = 20;
                var center_point = new Point((pitchFrame.Width / 2), (pitchFrame.Height / 2));
                var center_box_point = new Point((pitchFrame.Width / 2) - (center_size / 2), (pitchFrame.Height / 2) - (center_size / 2));
                Rectangle center = new Rectangle(center_box_point, new Size(center_size, center_size));

                //  CvInvoke.Rectangle(vspeed, center, new Bgr(Color.Red).MCvScalar, 1);


                if (center.Contains(line.P1) || center.Contains(line.P2))
                {

                    CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 2);

                    Point v_center_point;
                    Point other_point;

                    if (center.Contains(line.P1))
                    {
                        other_point = line.P2;
                        v_center_point = line.P1;
                    }
                    else
                    {
                        other_point = line.P1;
                        v_center_point = line.P2;
                    }

                    LineSegment2D baseLine = new LineSegment2D(new Point((pitchFrame.Width / 2), 0), new Point((pitchFrame.Width / 2), (pitchFrame.Height / 2)));

                  //  var center_full = new Point(v_center_point.X + base_rect.X, v_center_point.Y + base_rect.Y);
                  //  var other_full = new Point(other_point.X + base_rect.X, other_point.Y + base_rect.Y);
                  //
                   // CvInvoke.Line(data.Frame, center_full, other_full, new Bgr(Color.Black).MCvScalar, 3);
                   // CvInvoke.Line(data.Frame, center_full, other_full, new Bgr(Color.Lime).MCvScalar, 1);
                   // CvInvoke.Line(ProcessedFrame, v_center_point, other_point, new Bgr(Color.Yellow).MCvScalar, 2);



                    LineSegment2D small_line = new LineSegment2D(other_point, v_center_point);

                    var small_angle = Math2.FixAngle(Math2.angleBetween2Lines(small_line, baseLine), v_center_point, other_point);
                    small_angle -= 270;

                    // bias up to account for skew
                    small_angle += 2.75;

                    var dist = Math2.GetDistance(line.P1, line.P2);

                    if (dist > 63)
                    {
                        Trace.WriteLine("rejected pitch: " + dist);
                    }

                    //Trace.WriteLine("PITCH: " + Math.Round(ObservedValue) + " DIST: " + Math.Round(dist));

                    ObservedValue = small_angle;

                    return true;
                }
                else
                {
                    LastAction = "NOT CENTER -------------";
                }
            }

          //  else
            {
            //    LastAction = "######### No lines " + lines.len;
            }
        }
        else
        {
         //   IntermediateFrameBgr = markedup_circles;
            LastAction = "No main circle.";
        }
        return false;
    }
}
