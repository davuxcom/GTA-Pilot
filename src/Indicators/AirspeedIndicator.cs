using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GTAPilot.Indicators
{
    class AirspeedIndicator : ISimpleIndicator
    {
        public double CachedTuningValue => dyn_lower.CachedValue;
        public double LastGoodValue => Timeline.Speed;

        double last_value = 0;
        int num_rejected_values = 0;
        DateTime last_time = DateTime.Now;

        DynHsv dyn_lower = new DynHsv(0, 0, double.NaN, 0.04, 100);
        DynHsv dyn_lower2 = new DynHsv(0, 0, double.NaN, 0.0005, 100);


        public double ReadValue(IndicatorData data, ref object[] debugState)
        {
            DateTime this_time = DateTime.Now;
            DateTime this_last_time = last_time;
            double this_last_value = last_value;

            if (RollIndicator.TryFindRollCircleInFullFrame(data, out var circle))
            {
                circle.Center = new PointF(circle.Center.X + 140, circle.Center.Y + 70);
                circle.Radius = 55;
                var firstCrop = Math2.CropCircle(circle, 10);
                var focus = data.Frame.Copy(firstCrop);

                var vs_hsv = focus.Convert<Hsv, byte>();

                var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 20, 10, 180, 45, 55);
                if (circles.Length == 1)
                {
                    var circ = circles[0];
                    circ.Center = circles[0].Center.Add(firstCrop.Location);
                    circ.Radius = 50;
                    focus = data.Frame.Copy(Math2.CropCircle(circ, 10));

                    debugState[0] = focus;

                    vs_hsv = focus.Convert<Hsv, byte>();

                    var vs_blackimg = vs_hsv.DynLowInRange(dyn_lower, new Hsv(180, 255, 255)).PyrUp().PyrDown();

                    debugState[1] = vs_blackimg;

                    var d = (int)circ.Radius * 2;
                    var r = (int)circ.Radius;
                    var margin = 0;
                    var circCenter = new PointF(focus.Width / 2, focus.Height / 2);


                    //   var vs_blackimg2 = vs_blackimg.Copy(new Rectangle((int)circCenter.X - r - margin, (int)circCenter.Y - r - margin, d + margin * 2, d + margin * 2));
                    {
                          Mat vspeedMask = new Mat(vs_blackimg.Size, DepthType.Cv8U, 3);
                        {
                              vspeedMask.SetTo(new MCvScalar(1));
                              CvInvoke.Circle(vspeedMask, new Point(focus.Width / 2, focus.Height /2), (int)(r - 14), new Bgr(Color.White).MCvScalar, -1);
                            //
                            var vspeed_inner_only = focus.Copy(vspeedMask.ToImage<Gray, byte>()).Convert<Hsv, byte>();
                            {

                                var vspeed_inner_hsv = vspeed_inner_only.DynLowInRange(dyn_lower2, new Hsv(180, 255, 255));

                                    debugState[2] = vspeed_inner_hsv;

                                var cannyEdges3 = new Mat();
                                {
                                    // reference:
                                  //  CvInvoke.Canny(vspeed_inner_hsv, cannyEdges3, 10, 100);
                                  //  var lines = CvInvoke.HoughLinesP(cannyEdges3, 1, Math.PI / 45.0, 4, 14, 8).OrderByDescending(p => p.Length).ToList();


                                    CvInvoke.Canny(vspeed_inner_hsv, cannyEdges3, 20, 150);

                                    Mat dialatedCanny = new Mat();
                                 //   CvInvoke.Dilate(vspeed_inner_hsv, dialatedCanny, null, new Point(-1, -1), 1, BorderType.Default, new Gray(0).MCvScalar);


                                    var lines = CvInvoke.HoughLinesP(vspeed_inner_hsv, 1, Math.PI / 180, 20, 20, 4).OrderByDescending(p => p.Length).ToList();

                                    var center_size = 40;
                                    var center_point = new Point((focus.Width / 2), (focus.Height / 2));
                                    var center_box_point = new Point((focus.Width / 2) - (center_size / 2), (focus.Height / 2) - (center_size / 2));
                                    Rectangle center = new Rectangle(center_box_point, new Size(center_size, center_size));

                                    var bestLines = new List<Tuple<double, LineSegment2D>>();
                                    var markedup_frame = vs_blackimg.Convert<Bgr, byte>();

                                    debugState[3] = markedup_frame;
                                    {

                                        foreach (var line in lines)
                                        {
                                            CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 1);

                                            if (center.Contains(line.P1) || center.Contains(line.P2))
                                            {
                                                Point other_point;
                                                Point self_point;
                                                if (center.Contains(line.P1))
                                                {
                                                    other_point = line.P2;
                                                    self_point = line.P1;
                                                }
                                                else
                                                {
                                                    other_point = line.P1;
                                                    self_point = line.P2;
                                                }
                                                bestLines.Add(new Tuple<double, LineSegment2D>(Math2.GetDistance(other_point, self_point), line));
                                            }
                                        }
                                        bestLines = bestLines.OrderByDescending(l => l.Item1).ToList();

                                        CvInvoke.Rectangle(markedup_frame, center, new Bgr(Color.Yellow).MCvScalar, 1);

                                        List<double> ret = new List<double>();

                                        foreach (var lineX in bestLines)
                                        {
                                            var line = lineX.Item2;
                                            CvInvoke.Line(focus, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 2);
                                            CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);

                                            if (center.Contains(line.P1) || center.Contains(line.P2))
                                            {
                                                Point other_point;
                                                if (center.Contains(line.P1))
                                                {
                                                    other_point = line.P2;
                                                }
                                                else
                                                {
                                                    other_point = line.P1;
                                                }

                                                LineSegment2D final_line = new LineSegment2D(center_point, other_point);
                                                LineSegment2D baseLine = new LineSegment2D(new Point((focus.Width / 2), 0), new Point((focus.Width / 2), (focus.Height / 2)));

                                               // CvInvoke.Line(markedup_frame, baseLine.P1, baseLine.P2, new Bgr(Color.Purple).MCvScalar, 1);
                                               // CvInvoke.Line(markedup_frame, final_line.P1, final_line.P2, new Bgr(Color.Orange).MCvScalar, 1);


                                                var v_angle = Math2.angleBetween2Lines(line, baseLine);

                                                v_angle = v_angle * (180 / Math.PI);
                                                var v_angle_o = v_angle;

                                                if (v_angle == 180)
                                                {
                                                    var is_bottom = other_point.Y >= center_point.Y;
                                                    if (is_bottom) v_angle = 180;
                                                    else v_angle = 0;
                                                }
                                                else if (v_angle >= 180 && v_angle <= 270 && v_angle > 0)
                                                {
                                                    var is_bottom = other_point.Y >= center_point.Y && other_point.X <= center_point.X;
                                                    if (is_bottom)
                                                    {
                                                        // angle OK
                                                    }
                                                    else
                                                    {
                                                        v_angle -= 180;
                                                    }
                                                }
                                                else if (v_angle <= 0 && v_angle >= -90)
                                                {
                                                    var is_bottom = other_point.Y > center_point.Y || other_point.X > center_point.X;
                                                    if (is_bottom)
                                                    {
                                                        v_angle += 180;
                                                    }
                                                    else
                                                    {
                                                        v_angle += 360;
                                                    }
                                                }

                                                //  var center_full = new Point(center_point.X + base_rect.X, center_point.Y + base_rect.Y);
                                                //  var other_full = new Point(other_point.X + base_rect.X, other_point.Y + base_rect.Y);

                                                //  CvInvoke.Line(data.Frame, center_full, other_full, new Bgr(Color.Black).MCvScalar, 3);
                                                //  CvInvoke.Line(data.Frame, center_full, other_full, new Bgr(Color.Lime).MCvScalar, 1);

                                                //   CvInvoke.Line(ProcessedFrame, center_point, other_point, new Bgr(Color.Yellow).MCvScalar, 2);

                                                // 0-360 degrees
                                                // 0-180 knots

                                              //  Trace.WriteLine("SPEED: " + v_angle);

                                                var knots = (v_angle / 2);
                                                if (knots > 175) knots = 0;
                                                if (knots < 0)
                                                {
                                                    knots = 0;
                                                }

                                                return knots;
                                                //ret.Add(knots);
                                               // break;
                                             //   var ObservedValue = knots;

                                                /*
                                                var change = Math.Abs(ObservedValue - last_value);
                                                if (change > 40 && num_rejected_values < 10)
                                                {

                                                    num_rejected_values++;
                                                   // Trace.WriteLine("SPEED: reject");
                                                    return double.NaN;

                                                }

                                                last_value = ObservedValue;
                                                num_rejected_values = 0;

                                                CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);
                                                CvInvoke.Line(focus, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 2);
                                                break;
                                                */
                                            }
                                        }

                                        if (ret.Count == 0)
                                        {
                                          //  Trace.WriteLine("SPEED: -1 " + bestLines.Count);
                                            return double.NaN;
                                        }
                                       // return ret.Max();
                                       // return ret.Sum() / ret.Count;

                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //    Trace.WriteLine("SPEED: no circles(2)");
                }
            }
            return double.NaN;
        }
    }
}
