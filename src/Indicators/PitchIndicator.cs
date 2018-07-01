using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;

namespace GTAPilot.Indicators
{
    class PitchIndicator : ISimpleIndicator
    {
        public double CachedTuningValue => dyn_lower.CachedValue;
        public double LastGoodValue => Timeline.Pitch;

        DynHsv dyn_lower = new DynHsv(0, 0, double.NaN, 0.04, 100);

        public double ReadValue(IndicatorData data, DebugState debugState)
        {
            if (RollIndicator.TryFindRollCircleInFullFrame(data, out var circle))
            {
                circle.Center = new PointF(circle.Center.X, circle.Center.Y + 160);
                circle.Radius = 64;
                var firstCrop = Math2.CropCircle(circle, 25);
                var focus = data.Frame.Copy(firstCrop);

                var vs_hsv = focus.Convert<Hsv, byte>().PyrUp().PyrDown();

                var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 80, 10, 180, 60, 80);
                if (circles.Length == 1)
                {
                    var circ = circles[0];
                    circ.Center = circles[0].Center.Add(firstCrop.Location);
                    circ.Radius = 64;

                    focus = data.Frame.Copy(Math2.CropCircle(circ, 15));
                    debugState.Add(focus);
                    
                    vs_hsv = focus.Convert<Hsv, byte>();

                    var vs_blackimg = vs_hsv.DynLowInRange(dyn_lower, new Hsv(180, 255, 255));
                    debugState.Add(vs_blackimg);
                    
                    int margin = 10;

                    var vspeedMask = new Mat(focus.Size, DepthType.Cv8U, 3);
                    vspeedMask.SetTo(new MCvScalar(1));
                    CvInvoke.Circle(vspeedMask, Point.Round(new PointF(circles[0].Radius + margin, circles[0].Radius + margin)), (int)(circles[0].Radius - (circles[0].Radius * 0.1)), new Bgr(Color.White).MCvScalar, -1);

                    vs_blackimg = vs_blackimg.Copy(vspeedMask.ToImage<Gray, byte>());
                    var vspeed_inner_only = vs_blackimg.Copy(new Rectangle(0, 0, vs_blackimg.Width / 2, vs_blackimg.Height));

                    var cannyEdges3 = new Mat();
                    CvInvoke.Canny(vspeed_inner_only, cannyEdges3, 10, 140);
                    
                    debugState.Add(vspeed_inner_only);

                    LineSegment2D[] lines = CvInvoke.HoughLinesP(
                       vspeed_inner_only,
                       1, //Distance resolution in pixel-related units
                       Math.PI / 45.0, 20, 20, 14);

                    foreach (LineSegment2D line in lines)
                    {

                        CvInvoke.Line(focus, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);


                        //   var best_speed_line = lines.Take(1).First();
                        //  var line = lines.Take(1).First();

                        var center_size = 20;
                        var center_point = new Point((focus.Width / 2), (focus.Height / 2));
                        var center_box_point = new Point((focus.Width / 2) - (center_size / 2), (focus.Height / 2) - (center_size / 2));
                        Rectangle center = new Rectangle(center_box_point, new Size(center_size, center_size));

                        //  CvInvoke.Rectangle(vspeed, center, new Bgr(Color.Red).MCvScalar, 1);


                        if (center.Contains(line.P1) || center.Contains(line.P2))
                        {

                            CvInvoke.Line(focus, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 2);

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

                            LineSegment2D baseLine = new LineSegment2D(new Point((focus.Width / 2), 0), new Point((focus.Width / 2), (focus.Height / 2)));

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

                            return small_angle;
                        }
                    }
                }
            }
            return double.NaN;
        }
    }
}
