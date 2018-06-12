using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

public class RollIndicator : Indicator
{
    protected override InputType Type { get { return InputType.Roll; } }

    class PointAndCount
    {
        public Point point;
        public int count;
        public int state;
    }

    public RollIndicator()
    {
        TuningValues.Add(new Hsv(20, 0, 50));
        TuningValues.Add(new Hsv(20, 0, 85));
        TuningValues.Add(new Hsv(20, 0, 100));
        TuningValues.Add(new Hsv(20, 0, 150));
    }



    protected override Rectangle FastROI
    {
        get
        {
            if (Hints.AttitudeIndicator.Center.X > 0)
            {
                return Math2.CropCircle(Hints.AttitudeIndicator, 20);
            }
            else
            {
                return default(Rectangle);
            }
        }
    }

    protected override bool ProcessFrameCore(IndicatorData data, ref double ObservedValue)
    {
        var gray_frame = new Mat();
        CvInvoke.CvtColor(data.Frame, gray_frame, ColorConversion.Bgr2Gray);

        var circles = CvInvoke.HoughCircles(gray_frame, HoughType.Gradient, 2.0, 20, 10, 180, 60, 80);
        if (!data.IsFastROI && circles.Length == 0)
        {
            circles = CvInvoke.HoughCircles(gray_frame, HoughType.Gradient, 2.0, 80, 10, 80, 60, 80);
            // note had check for bottom 3rd here
        }

        // Convert to frame cords
        var newCircles = new List<CircleF>();
        foreach (var c in circles)
        {
            var newCircle = new CircleF(new PointF(c.Center.X + data.ROI.X, c.Center.Y + data.ROI.Y), c.Radius);
            newCircles.Add(newCircle);
        }
        circles = newCircles.ToArray();

        if (circles.Length > 0)
        {
            CircleF circle = default(CircleF);

            if (circles.Length == 1) circle = circles[0];
            else if (circles.Length == 2)
            {
                circle = (circles[0].Center.Y < circles[1].Center.Y) ? circles[0] : circles[1];
            }
            else
            {
                LastAction = "Circles wrong: " + circles.Length;
                return false;
            }

            // Save hint for others
            Hints.AttitudeIndicator = circle;

            // Translate from fullframe to ROI
            circle = new CircleF(new PointF(circle.Center.X - data.ROI.X, circle.Center.Y - data.ROI.Y), circle.Radius);
            var circle_rect = Math2.CropCircle(circle, 2);
            // translate from ROI to new ROI
            circle = new CircleF(new PointF(circle.Center.X - circle_rect.X, circle.Center.Y - circle_rect.Y), circle.Radius);


            var rollFrame = data.Frame.Copy(circle_rect);

            Mat maskInnerAlt = new Mat(rollFrame.Size, DepthType.Cv8U, 3);
            maskInnerAlt.SetTo(new MCvScalar(1));

            CvInvoke.Circle(maskInnerAlt, new Point(rollFrame.Size.Width / 2, rollFrame.Size.Height / 2), (int)(circle.Radius - (circle.Radius * 0.2)), new Bgr(Color.White).MCvScalar, -1);
            CvInvoke.Circle(maskInnerAlt, new Point(rollFrame.Size.Width / 2, rollFrame.Size.Height / 2), (int)(circle.Radius - (circle.Radius * 0.275)), new Bgr(Color.Black).MCvScalar, -1);

            var rim_only = rollFrame.Copy(maskInnerAlt.ToImage<Gray, byte>());

            var rim_only2 = rim_only.Copy(new Rectangle(0, 0, rim_only.Width, (int)(rim_only.Height - (rim_only.Height * 0.235))));

            var hsv = rim_only2.Convert<Hsv, byte>();

            var ring_hsv = hsv.InRange(TuningValue, new Hsv(180, 255, 255));




            Mat maskInner = new Mat(rollFrame.Size, DepthType.Cv8U, 3);
            maskInner.SetTo(new MCvScalar(1));

            CvInvoke.Circle(maskInner, new Point(rollFrame.Size.Width / 2, rollFrame.Size.Height / 2), (int)(circle.Radius - (circle.Radius * 0.45)), new Bgr(Color.White).MCvScalar, -1);
           // CvInvoke.Circle(maskInner, new Point(rollFrame.Size.Width / 2, rollFrame.Size.Height / 2), (int)(circle.Radius - (circle.Radius * 0.275)), new Bgr(Color.Black).MCvScalar, -1);

            var inner = rollFrame.Copy(maskInner.ToImage<Gray, byte>());
            var inner_hsv1 = inner.Convert<Hsv, byte>();
            var inner_hsv = inner_hsv1.InRange(TuningValue, new Hsv(180, 255, 255));
            inner_hsv = inner_hsv.PyrUp().PyrDown();
            var cannyEdges3 = new Mat();

            CvInvoke.Canny(inner_hsv, cannyEdges3, 1,100);


            var markedup_frame = cannyEdges3;

            var lines = CvInvoke.HoughLinesP(
   cannyEdges3,
   1, Math.PI / 45.0, 1, 30, 12);

            lines = lines.OrderByDescending(l => l.Length).ToArray();

            foreach (var line in lines)
            {

                CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);
            }

            if (lines.Length > 0)
            {
                var left_point = lines[0].P1;
                var right_point = lines[0].P2; //.Y > lines[0].P2.Y ? lines[0].P1 : lines[0].P2;


                CvInvoke.Line(markedup_frame,
                    left_point,
                    right_point,
                    new Bgr(Color.Yellow).MCvScalar, 1);

                LineSegment2D baseLine = new LineSegment2D(new Point((markedup_frame.Width / 2), 0), new Point((markedup_frame.Width / 2), (markedup_frame.Height / 2)));
                LineSegment2D small_line = new LineSegment2D(left_point, right_point);

                var small_angle = Math2.FixAngle(Math2.angleBetween2Lines(small_line, baseLine), right_point, left_point);
                small_angle -= 270;


            var my_frameref = Timeline.Data[data.Id];

                my_frameref.Extended.RollBias = small_angle;

                
               // Trace.WriteLine("BIAS: " + small_angle);
            }

            IntermediateFrameGray = markedup_frame.ToImage<Gray, byte>();

            CvBlobs small_blobs = new CvBlobs();
            GetBlobDetector().Detect(ring_hsv, small_blobs);
            small_blobs.FilterByArea(1, 500);

            Mat maskBlobs = new Mat(ring_hsv.Size, DepthType.Cv8U, 3);

            maskBlobs.SetTo(new Bgr(Color.White).MCvScalar);

            foreach (var b in small_blobs)
            {
                CvInvoke.Rectangle(maskBlobs, b.Value.BoundingBox, new Bgr(Color.Black).MCvScalar, -1);
            }

            var blobImage = maskBlobs.ToImage<Gray, byte>();

            var ring_hsv2 = ring_hsv.Copy(blobImage);

            var ring_distance_transform = new Image<Gray, float>(ring_hsv2.Size);


            CvInvoke.DistanceTransform(ring_hsv2, ring_distance_transform, null, DistType.L1, 3);
            circles = CvInvoke.HoughCircles(ring_hsv2, HoughType.Gradient, 2.0, 1.0, 1, 10, 50, 53);

            var boundaries = new List<PointAndCount>();
            if (circles.Length > 0)
            {
                var cir = circles[0];
                var radius = cir.Radius;

                int state = 0;
                int FOUND = 1;
                int NOTFOUND = 2;
                Point last = new Point(0, 0);
                int found_count = 0;

                for (float t = 0; t < 2 * Math.PI; t += 0.05f)
                {
                    var cX_ = (int)(radius * Math.Cos(t) + cir.Center.X);
                    var cY_ = (int)(radius * Math.Sin(t) + cir.Center.Y);

                    Func<int, int, bool> check = (int cX, int cY) =>
                    {
                        if (cX < ring_distance_transform.Width && cX > 0 &&
                        cY < ring_distance_transform.Height && cY > 0)
                        {
                            var b = ring_distance_transform[cY, cX];
                            return (b.Intensity > 0);
                        }
                        return false;
                    };

                    int pos = 1;
                    int xpos = 2;
                    int xpos2 = 3;
                    int xpos3 = 4;
                    int xpos4 = 5;
                    int xpos5 = 6;
                    int xpos6 = 7;
                    var current = Point.Round(new Point(cX_, cY_));
                    if (check(cX_, cY_) ||
                         check(cX_ + xpos, cY_) ||
                         check(cX_ + xpos2, cY_) ||
                         check(cX_ + xpos3, cY_) ||
                         check(cX_ + xpos4, cY_) ||
                         check(cX_ + xpos5, cY_) ||
                         check(cX_ + xpos6, cY_) ||
                         check(cX_ - xpos, cY_) ||
                         check(cX_ - xpos2, cY_) ||
                         check(cX_ - xpos3, cY_) ||
                         check(cX_ - xpos4, cY_) ||
                         check(cX_ - xpos5, cY_) ||
                         check(cX_ - xpos6, cY_) ||
                         check(cX_, cY_ + pos) ||
                         check(cX_, cY_ - pos))
                    {
                        found_count++;

                        if (state == NOTFOUND)
                        {
                            boundaries.Add(new PointAndCount { point = current, count = found_count, state = FOUND });
                        }
                        state = FOUND;
                    }
                    else
                    {
                        found_count = 0;
                        if (state == FOUND)
                        {
                            boundaries.Add(new PointAndCount { point = last, count = found_count, state = NOTFOUND });
                        }
                        state = NOTFOUND;
                    }
                    last = current;
                }

                if (state == boundaries[0].state)
                {
                    boundaries.Add(new PointAndCount { point = last, count = found_count, state = state });
                }

                Point boundary_one = default(Point);
                Point boundary_two = default(Point);
                if (boundaries.Count == 2)
                {
                    boundary_one = boundaries[0].point;
                    boundary_two = boundaries[1].point;
                }
                else if (boundaries.Count > 2)
                {
                    int highest = 0;
                    for (int i = 0; i < boundaries.Count; ++i)
                    {
                        if (boundaries[i].count > highest)
                        {
                            if (i == 0)
                            {
                                boundary_one = boundaries[0].point;
                                boundary_two = boundaries[boundaries.Count - 1].point;
                            }
                            else
                            {
                                boundary_one = boundaries[i].point;
                                boundary_two = boundaries[i - 1].point;
                            }
                        }
                    }
                }
                else
                {
                    Trace.WriteLine("############# RESET");
                    Hints.AttitudeIndicator = default(CircleF);
                    LastAction = "No boundary: " + boundaries.Count;
                    return false;
                }

                var dist = Math2.GetDistance(boundary_one, boundary_two);
                if (dist < 88 || dist > 140)
                {
                    LastAction = "Invalid indicator line distance: " + dist;
                    return false;
                }

                var line = new LineSegment2D(boundary_one, boundary_two);
                LineSegment2D baseLine2 = new LineSegment2D(new Point(0, ring_hsv2.Height), new Point(0, 0));
                var angle = line.GetExteriorAngleDegree(baseLine2);

                // 90 -> 0 RIGHT
                // -90 -> 0 LEFT

                if (angle > 0)
                {
                    angle -= 90;
                    angle *= -1;
                }
                else if (angle < 0)
                {
                    angle += 90;
                    angle *= -1;
                }

                angle += 1.5;

                ObservedValue = angle;

                //  Stopwatch w = new Stopwatch();

                //  Trace.WriteLine("Roll computing..." + w.ElapsedMilliseconds);
                failed_final = 0;
                return true;
            }
            else
            {
                LastAction = "No small circles";
            }
        }
        else
        {
            IntermediateFrameGray = gray_frame.ToImage<Gray, byte>();
            LastAction = "No circles from large frame.";
        }

        failed_final++;
     //   Trace.WriteLine("#############");
        if (failed_final > 1 || !data.IsFastROI)
        {
           // Trace.WriteLine("############# ---- reset ----");
          //  SavedFrames.TryAdd(data.Id, data);

            Hints.AttitudeIndicator = default(CircleF);
        }
        return false;
    }

    int failed_final = 0;
}

