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


        int LastBig = 0;

        DynHsv dyn_lower = new DynHsv(0, 0, double.NaN, 0.04, 100);

        public double ReadValue(IndicatorData data, ref object[] debugState)
        {
            if (RollIndicator_v2.TryFindRollCircleInFullFrame(data.Frame, out var circle))
            {
                circle.Center = new PointF(circle.Center.X + 140, circle.Center.Y - 40);
                circle.Radius = 45;
                var firstCrop = Math2.CropCircle(circle, 25);
                var focus = data.Frame.SafeCopy(firstCrop);

                var vs_hsv = focus.Convert<Hsv, byte>(); //.PyrUp().PyrDown();

                var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 80, 4, 50, 40, 50);
                if (circles.Length == 1)
                {
                    var circ = circles[0];
                    circ.Center = circles[0].Center.Add(firstCrop.Location);
                    circ.Radius = 45;

                    focus = data.Frame.SafeCopy(Math2.CropCircle(circ, 15));
                    debugState[0] = focus;
                    vs_hsv = focus.Convert<Hsv, byte>();

                    var vs_blackimg = vs_hsv.DynLowInRange(dyn_lower, new Hsv(180, 255, 255));

                    debugState[1] = vs_blackimg;


                    int margin = 0;
                    var d = (int)circ.Radius * 2;
                    var r = (int)circ.Radius;

                  //  Mat vspeedMask = new Mat(vs_blackimg.Size, DepthType.Cv8U, 3);
                  //  vspeedMask.SetTo(new MCvScalar(1));
                  //  CvInvoke.Circle(vspeedMask, Point.Round(new PointF(r + margin, r + margin)), (int)(r - (r * 0)), new Bgr(Color.White).MCvScalar, -1);

                   // vs_blackimg = vs_blackimg.Copy(vspeedMask.ToImage<Gray, byte>());

                    var markedup_frame = vs_blackimg.Convert<Bgr, byte>();

                    debugState[2] = markedup_frame;

                    // IntermediateFrameGray = vs_blackimg;

                    var cannyEdges3 = new Mat();
                    CvInvoke.Canny(vs_blackimg, cannyEdges3, 10, 40);
                    var lines = CvInvoke.HoughLinesP(
                       cannyEdges3,
                       1, Math.PI / 45.0, 12, 24, 3);

                    lines = lines.OrderByDescending(l => l.Length).ToArray();

                    foreach (var line in lines)
                    {

                        CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);
                    }

                    foreach (var line in lines)
                    {

                        //  CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);

                        var center_size = 25;
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
                            CvInvoke.Line(focus, center_point, other_point, new Bgr(Color.Yellow).MCvScalar, 2);

                            // ground
                            //  if (small_angle < 30 && LastBig == 0) hundreds = 0;


                            //  small_angle = Math.Round((small_angle / 360) * 10);
                            //    IntermediateFrameBgr = markedup_frame;



                            
                          //  int the_big = 0;
                          //  if (hundreds >= 5) the_big = (int)Math.Floor(LastBig);
                          //  else the_big = (int)Math.Ceiling(LastBig);
                          //  */
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
                            */


                            //   int the_big = (int)Math.Floor(LastBig);


                            

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


                            // TODO: disabled this change code, logic was pretty soudn though iirc
                            
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
                                     //   num_rejected_values++;
                                        //   Trace.WriteLine("rejected altitude value: " + nextValue + " " + increasedNextValue + " " + decresedNextValue);
                                        // ????
                                        return double.NaN;
                                    }


                            }


                            //  var num_rejected_values = 0;


                            //   if (nextValue < 35) return double.NaN;

                            /*
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
                            */
                            // ObservedValue = nextValue;


                            /*
                            if (change > 300 && num_rejected_values < 10)
                            {
                                num_rejected_values++;
                               // Trace.WriteLine("Rejected altitude change due to size: " + change + " (stay at " + last_value + ")");
                                return false;
                            }

                       //     reset:
                       */
                            //last_value = ObservedValue;
                            //  num_rejected_values = 0;

                            //  Trace.WriteLine("ALT: " + ObservedValue + " " + LastBig + " " + hundreds);
                            last_value = nextValue;
                            return nextValue;
                        }
                    }
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
