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
    class RollIndicator : ISimpleIndicator
    {
        class PointAndCount
        {
            public Point point;
            public int count;
            public int state;
        }

        static Rectangle MovementRect = new Rectangle(600, 450, (Metrics.Frame.Width / 2) - 600 - 100, Metrics.Frame.Height - 450 - 250);

        public double CachedTuningValue => 0;
        public double LastGoodValue => Timeline.Roll;

        public double ReadValue(IndicatorData data, DebugState debugState)
        {
            if (TryFindRollCircleInFullFrame(data, out CircleF rollIndicatorCicle))
            {
                var FocusRect = Math2.CropCircle(rollIndicatorCicle, 10);
                var focusFrame = data.Frame.SafeCopy(FocusRect);

                debugState.Add(focusFrame);

                // Isolate the outside ring
                Mat maskInnerAlt = new Mat(focusFrame.Size, DepthType.Cv8U, 3);
                maskInnerAlt.SetTo(new MCvScalar(1));

                CvInvoke.Circle(maskInnerAlt, new Point(focusFrame.Size.Width / 2, focusFrame.Size.Height / 2), (int)(rollIndicatorCicle.Radius - (rollIndicatorCicle.Radius * 0.2)), new Bgr(Color.White).MCvScalar, -1);
                CvInvoke.Circle(maskInnerAlt, new Point(focusFrame.Size.Width / 2, focusFrame.Size.Height / 2), (int)(rollIndicatorCicle.Radius - (rollIndicatorCicle.Radius * 0.275)), new Bgr(Color.Black).MCvScalar, -1);

                var outerMovingRingOnly = focusFrame.Copy(maskInnerAlt.ToImage<Gray, byte>());
                var outerMovingRingWithoutBottom = outerMovingRingOnly.Copy(new Rectangle(0, 0, outerMovingRingOnly.Width, (int)(outerMovingRingOnly.Height ))); // - (outerMovingRingOnly.Height * 0.29)

                var hsv = outerMovingRingWithoutBottom.Convert<Hsv, byte>();

                debugState.Add(outerMovingRingOnly);
                debugState.Add(outerMovingRingWithoutBottom);

                // Low is TuningValue
                var ring_hsv_unfiltered = hsv.InRange(new Hsv(20, 0, 85), new Hsv(180, 255, 255));
                debugState.Add(ring_hsv_unfiltered);

                var ring_hsv = Utils.RemoveBlobs(ring_hsv_unfiltered, 1, 200);
                debugState.Add(ring_hsv);


                var ring_distance_transform = new Image<Gray, float>(ring_hsv.Size);

                CvInvoke.DistanceTransform(ring_hsv, ring_distance_transform, null, DistType.L1, 3);


                var circles = CvInvoke.HoughCircles(ring_hsv, HoughType.Gradient, 2.0, 1.0, 1, 10, 50, 53);

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

                    for (double t = Math.PI / 2; t < Math.PI * 3; t += 0.05f)
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
                        int xpos3 = 5;
                        int xpos4 = 3;
                        int xpos5 = 3;
                        int xpos6 = 3;
                        var current = Point.Round(new Point(cX_, cY_));

                        // TODO: Simplify this condition/calculation
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
                            //CvInvoke.Circle(focusFrame, new Point(cX_, cY_), 1, new Bgr(Color.Green).MCvScalar, 1);

                            if (state == NOTFOUND)
                            {
                                boundaries.Add(new PointAndCount { point = current, count = found_count, state = FOUND });
                            }
                            state = FOUND;
                        }
                        else
                        {
                            //CvInvoke.Circle(focusFrame, new Point(cX_, cY_), 1, new Bgr(Color.Red).MCvScalar, 1);
                            found_count = 0;
                            if (state == FOUND)
                            {
                                boundaries.Add(new PointAndCount { point = last, count = found_count, state = NOTFOUND });
                            }
                            state = NOTFOUND;
                        }
                        last = current;
                    }

                    // NOTE: saw this crash with boundaries.count==0
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
                        boundary_one = boundaries.First(b => b.state == FOUND).point;
                        boundary_two = boundaries.Last(b => b.state == NOTFOUND).point;
                    }
                    else
                    {
                      //  Trace.WriteLine($"ROLL: boundaries {boundaries.Count}");
                        return double.NaN;
                    }

                    var dist = Math2.GetDistance(boundary_one, boundary_two);
                    if (dist < 88 || dist > 140)
                    {
                      //  Trace.WriteLine($"ROLL: Dist {dist}");
                        return double.NaN;
                    }

                    var line = new LineSegment2D(boundary_one, boundary_two);
                    LineSegment2D baseLine2 = new LineSegment2D(new Point(0, ring_hsv.Height), new Point(0, 0));
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
                    
                    CvInvoke.Line(focusFrame, boundary_one, boundary_two, new Bgr(Color.Yellow).MCvScalar, 2);

                    // skew considered from other panels
                    return angle + 1;
                }
                else
                {
                  //  Trace.WriteLine($"ROLL: boundary circles");
                }
            }
            else
            {
              //  Trace.WriteLine($"ROLL: Main circles");
            }
            return double.NaN;
        }

        public static bool TryFindRollCircleInFullFrame(IndicatorData data, out CircleF ret)
        {
            ret = default(CircleF);

            if (Timeline.Data[data.Id].Roll.ForIndicatorUse != null)
            {
                // We found our own hint, return it.
                ret = (CircleF)Timeline.Data[data.Id].Roll.ForIndicatorUse;
                return true;
            }

            var localRect = MovementRect;

            if (data.Id > 0 && Timeline.Data[data.Id - 1].Roll.ForIndicatorUse != null)
            {
                // Our hint from last time.
                ret = (CircleF)Timeline.Data[data.Id - 1].Roll.ForIndicatorUse;

                localRect = Math2.CropCircle(ret, 10);
            }

            // Crop and blur
            var cropped_frame = data.Frame.Copy(localRect).PyrUp().PyrDown();

            var MovementFrameGray = new Mat();
            CvInvoke.CvtColor(cropped_frame, MovementFrameGray, ColorConversion.Bgr2Gray);

            // Locate the attitude and possibly vertical speed indicators.
            var circles = CvInvoke.HoughCircles(MovementFrameGray, HoughType.Gradient, 2.0, 20, 10, 180, 60, 80);
            if (circles.Length == 0)
            {
                // Couldn't find initial circle
                return false;
            }

            // Pick the topmost circle and crop.
            var rollIndicatorCicle = circles.OrderBy(c => c.Center.Y).First();
            rollIndicatorCicle.Radius = 64;
            rollIndicatorCicle.Center = rollIndicatorCicle.Center.Add(localRect.Location);
            ret = rollIndicatorCicle;
            Timeline.Data[data.Id].Roll.ForIndicatorUse = ret;
            return true;
        }
    }
}
