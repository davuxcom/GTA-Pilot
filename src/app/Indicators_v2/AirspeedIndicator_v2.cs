using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GTAPilot.Indicators_v2
{
    class AirspeedIndicator_v2 : ISimpleIndicator
    {
        public double CachedTuningValue => dyn_lower.CachedValue;

        double last_value = 0;
        int num_rejected_values = 0;
        DateTime last_time = DateTime.Now;

        DynHsv dyn_lower = new DynHsv(0, 0, double.NaN, 0.04, 100);


        public double ReadValue(IndicatorData data, ref object[] debugState)
        {
            DateTime this_time = DateTime.Now;
            DateTime this_last_time = last_time;
            double this_last_value = last_value;

            if (RollIndicator_v2.TryFindRollCircleInFullFrame(data.Frame, out var circle))
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
                    // TODO: tune low ?
                    var vs_blackimg = vs_hsv.DynLowInRange(dyn_lower, new Hsv(180, 255, 255));

                    debugState[1] = vs_blackimg;

                    var margin = 4;
                    var d = (int)circ.Radius * 2;
                    var r = (int)circ.Radius;

                    var circCenter = new PointF(focus.Width / 2, focus.Height / 2);


                    var vs_blackimg2 = vs_blackimg.Copy(new Rectangle((int)circCenter.X - r - margin, (int)circCenter.Y - r - margin, d + margin * 2, d + margin * 2));
                    {
                        Mat vspeedMask = new Mat(vs_blackimg2.Size, DepthType.Cv8U, 3);
                        {
                            vspeedMask.SetTo(new MCvScalar(1));
                            CvInvoke.Circle(vspeedMask, Point.Round(new PointF(r + margin, r + margin)), (int)(r - (r * 0)), new Bgr(Color.White).MCvScalar, -1);

                            var vspeed_inner_only = vs_blackimg2.Copy(vspeedMask.ToImage<Gray, byte>());
                            {

                                debugState[2] = vspeed_inner_only;

                                var cannyEdges3 = new Mat();
                                {
                                    CvInvoke.Canny(vspeed_inner_only, cannyEdges3, 10, 100);
                                    var lines = CvInvoke.HoughLinesP(cannyEdges3, 1, Math.PI / 45.0, 4, 14, 4).OrderByDescending(p => p.Length).ToList();

                                    var center_size = 25;
                                    var center_point = new Point((focus.Width / 2), (focus.Height / 2));
                                    var center_box_point = new Point((focus.Width / 2) - (center_size / 2), (focus.Height / 2) - (center_size / 2) + 4);
                                    Rectangle center = new Rectangle(center_box_point, new Size(center_size, center_size));

                                    var bestLines = new List<Tuple<double, LineSegment2D>>();
                                    var markedup_frame = vspeed_inner_only.Convert<Bgr, byte>();

                                    debugState[3] = markedup_frame;
                                    {

                                        foreach (var line in lines)
                                        {
                                            CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 1);

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
                                                bestLines.Add(new Tuple<double, LineSegment2D>(Math2.GetDistance(other_point, center_point), line));
                                            }
                                        }
                                        bestLines = bestLines.OrderByDescending(l => l.Item1).ToList();

                                        CvInvoke.Rectangle(markedup_frame, center, new Bgr(Color.Yellow).MCvScalar, 1);

                                        bool didFinish = false;
                                        double ObservedValue = -1;
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

                                                var v_angle = Math2.angleBetween2Lines(line, baseLine);
                                                v_angle = v_angle * (180 / Math.PI);
                                                var v_angle_o = v_angle;
                                                if (v_angle >= 180 && v_angle <= 270 && v_angle > 0)
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

                                                var knots = (v_angle / 2);
                                                if (knots > 175) knots = 0;

                                                ObservedValue = knots;

                                                var change = Math.Abs(ObservedValue - last_value);
                                                if (change > 40 && num_rejected_values < 10)
                                                {
                                                    num_rejected_values++;
                                                    return double.NaN;
                                                }

                                                last_value = ObservedValue;
                                                num_rejected_values = 0;

                                                didFinish = true;
                                                CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);
                                                CvInvoke.Line(focus, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 2);
                                                break;
                                            }
                                        }

                                        return ObservedValue;
                                    }
                                }
                            }
                        }
                    }



                    debugState = new object[] { vs_blackimg };

                    return 0;
                }
                else
                {
                    return double.NaN;
                }

            }

            return double.NaN;
        }

            /*



            var vs_hsv = frame.Convert<Hsv, byte>();
            {
                var markedup_circles = vs_hsv[2].Convert<Bgr, byte>();
                debugState = new object[] { markedup_circles };

                return double.NaN;









                {
                    var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 20, 10, 180, 30, 100);
                    if (circles.Length == 0)
                    {
                        circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 20, 10, 100, 30, 100);
                        foreach (var c in circles)
                        {
                            CvInvoke.Circle(markedup_circles, Point.Round(c.Center), (int)c.Radius, new Bgr(Color.Red).MCvScalar, 1);
                        }
                    }
                    if (circles.Length > 0)
                    {
                        var airspeed_circle = circles[0];
                        var best_circle = airspeed_circle;

                        //  var vs_hsv2 = ProcessedFrame.Convert<Hsv, byte>())
                        {
                            // TODO: Low is TuningValue
                            var vs_blackimg = vs_hsv.InRange(new Hsv(0, 0, 140), new Hsv(180, 255, 255));
                            {
                                circles = CvInvoke.HoughCircles(vs_blackimg, HoughType.Gradient, 2.0, 40, 10, 20, 40, 50);
                                if (circles.Length > 0)
                                {

                                    var margin = 4;
                                    best_circle = circles[0];
                                    var d = (int)best_circle.Radius * 2;
                                    var r = (int)best_circle.Radius;


                                    var new_rect = new Rectangle((int)best_circle.Center.X - r - margin, (int)best_circle.Center.Y - r - margin, d + margin * 2, d + margin * 2);

                                    var speedFrame = frame.Copy(new_rect);


                                    var vs_blackimg2 = vs_blackimg.Copy(new Rectangle((int)best_circle.Center.X - r - margin, (int)best_circle.Center.Y - r - margin, d + margin * 2, d + margin * 2));
                                    {
                                        Mat vspeedMask = new Mat(vs_blackimg2.Size, DepthType.Cv8U, 3);
                                        {
                                            vspeedMask.SetTo(new MCvScalar(1));
                                            CvInvoke.Circle(vspeedMask, Point.Round(new PointF(r + margin, r + margin)), (int)(r - (r * 0)), new Bgr(Color.White).MCvScalar, -1);

                                            var vspeed_inner_only = vs_blackimg2.Copy(vspeedMask.ToImage<Gray, byte>());
                                            {

                                                var cannyEdges3 = new Mat();
                                                {
                                                    CvInvoke.Canny(vspeed_inner_only, cannyEdges3, 10, 100);
                                                    var lines = CvInvoke.HoughLinesP(cannyEdges3, 1, Math.PI / 45.0, 4, 14, 4).OrderByDescending(p => p.Length).ToList();

                                                    var center_size = 25;
                                                    var center_point = new Point((speedFrame.Width / 2), (speedFrame.Height / 2));
                                                    var center_box_point = new Point((speedFrame.Width / 2) - (center_size / 2), (speedFrame.Height / 2) - (center_size / 2) + 4);
                                                    Rectangle center = new Rectangle(center_box_point, new Size(center_size, center_size));

                                                    var bestLines = new List<Tuple<double, LineSegment2D>>();
                                                    var markedup_frame = vspeed_inner_only.Convert<Bgr, byte>();
                                                    {

                                                        foreach (var line in lines)
                                                        {
                                                            CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 1);

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
                                                                bestLines.Add(new Tuple<double, LineSegment2D>(Math2.GetDistance(other_point, center_point), line));
                                                            }
                                                        }
                                                        bestLines = bestLines.OrderByDescending(l => l.Item1).ToList();

                                                        CvInvoke.Rectangle(markedup_frame, center, new Bgr(Color.Yellow).MCvScalar, 1);

                                                        bool didFinish = false;
                                                        double ObservedValue = -1;
                                                        foreach (var lineX in bestLines)
                                                        {
                                                            var line = lineX.Item2;
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
                                                                LineSegment2D baseLine = new LineSegment2D(new Point((speedFrame.Width / 2), 0), new Point((speedFrame.Width / 2), (speedFrame.Height / 2)));

                                                                var v_angle = Math2.angleBetween2Lines(line, baseLine);
                                                                v_angle = v_angle * (180 / Math.PI);
                                                                var v_angle_o = v_angle;
                                                                if (v_angle >= 180 && v_angle <= 270 && v_angle > 0)
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

                                                                var knots = (v_angle / 2);
                                                                if (knots > 175) knots = 0;

                                                                ObservedValue = knots;

                                                                var change = Math.Abs(ObservedValue - last_value);
                                                                if (change > 40 && num_rejected_values < 10)
                                                                {
                                                                    num_rejected_values++;
                                                                    return double.NaN;
                                                                }

                                                                last_value = ObservedValue;
                                                                num_rejected_values = 0;

                                                                didFinish = true;
                                                                CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);
                                                                break;
                                                            }
                                                        }

                                                        debugState = new object[] { markedup_frame };

                                                        return ObservedValue;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    return double.NaN;
                                }
                            }
                        }
                    }
                    else
                    {
                        // IntermediateFrameBgr = markedup_circles;
                    }
                    return double.NaN;
                }
            }
        }
        */
    }
}
