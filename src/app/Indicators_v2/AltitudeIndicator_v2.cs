using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace GTAPilot.Indicators_v2
{
    class AltitudeIndicator_v2 : ISimpleIndicator
    {
        public double CachedTuningValue => dyn_lower.CachedValue;
        public double LastGoodValue => Timeline.Altitude;

        DynHsv dyn_lower = new DynHsv(0, 0, double.NaN, 0.01, 100);

        int LastBig = 0;

        public double ReadValue(IndicatorData data, ref object[] debugState)
        {
            if (RollIndicator_v2.TryFindRollCircleInFullFrame(data, out var circle))
            {
                circle.Center = new PointF(circle.Center.X + 140, circle.Center.Y - 40);
                circle.Radius = 45;
                var firstCrop = Math2.CropCircle(circle, 25);
                var focus = data.Frame.SafeCopy(firstCrop);

                var vs_hsv = focus.Convert<Hsv, byte>().PyrUp().PyrDown();

                var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 80, 4, 50, 40, 50);
                if (circles.Length == 1)
                {
                    var circ = circles[0];
                    circ.Center = circles[0].Center.Add(firstCrop.Location);
                    circ.Radius = 45;

                    focus = data.Frame.SafeCopy(Math2.CropCircle(circ, 15));
                    debugState[0] = focus;
                    vs_hsv = focus.Convert<Hsv, byte>();

                    var vs_blackimg = vs_hsv.DynLowInRange(dyn_lower, new Hsv(180, 255, 255)).PyrUp().PyrDown();

                    debugState[1] = vs_blackimg;



                    var d = (int)circ.Radius * 2;
                    var r = (int)circ.Radius;

                    var markedup_frame = vs_blackimg.Convert<Bgr, byte>();

                    debugState[2] = markedup_frame;

                    var cannyEdges3 = new Mat();
                    CvInvoke.Canny(vs_blackimg, cannyEdges3, 10, 100);
                    var lines = CvInvoke.HoughLinesP(
                       cannyEdges3,
                       1, Math.PI / 45.0, 4, 14, 4);

                    lines = lines.OrderByDescending(l => l.Length).ToArray();

                    if (lines.Length == 0)
                    {
                      //  Trace.WriteLine("NO LINES");
                    }

                    foreach (var line in lines)
                    {
                        CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);
                    }

                    foreach (var line in lines)
                    {

                        //  CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);

                        var center_size = 35;
                        var center_point = new Point((focus.Width / 2) - 4, (focus.Height / 2) + 4);
                        var center_box_point = new Point((focus.Width / 2) - (center_size / 2), 4 + (focus.Height / 2) - (center_size / 2));
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
                                new Point((focus.Width / 2), 0), new Point((focus.Width / 2), (focus.Height / 2))

                                );


                            center_point = real_center_point;

                            LineSegment2D small_line = new LineSegment2D(other_point, center_point);

                            var small_angle = Math2.FixAngle(Math2.angleBetween2Lines(small_line, baseLine), center_point, other_point);


                            var hundreds = Math.Round((small_angle / 360) * 1000);

                            CvInvoke.Line(markedup_frame, center_point, other_point, new Bgr(Color.Purple).MCvScalar, 1);
                            CvInvoke.Line(focus, center_point, other_point, new Bgr(Color.Yellow).MCvScalar, 2);

                            

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

                            if (Math.Abs(nextValue - last_value) > max_change)
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
                                     //  num_rejected_values++;
                                      //    Trace.WriteLine("rejected altitude value: " + nextValue + " " + increasedNextValue + " " + decresedNextValue);
                                        // ????
                                        return double.NaN;
                                    }


                            }

                           
                            last_value = nextValue;
                            return nextValue;
                        }
                    }

                  //  Trace.WriteLine("NO CENTER LINES");
                }
                else
                {
                //    Trace.WriteLine("Didn't find cicles 2");
                }
            }
            return double.NaN;
        }

        double last_value = 0;

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
    }
}
