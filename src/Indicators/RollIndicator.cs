using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
using System.Drawing;
using System.Linq;

namespace GTAPilot.Indicators
{
    class RollIndicator : ISimpleIndicator
    {
        static Rectangle MovementRect = new Rectangle(600, 450, (Metrics.Frame.Width / 2) - 600 - 100, Metrics.Frame.Height - 450 - 250);

        public double CachedTuningValue => 0;
        public double LastGoodValue => Timeline.Roll;

        public double ReadValue(IndicatorData data, DebugState debugState)
        {
            if (TryFindRollCircleInFullFrame(data, out CircleF rollIndicatorCicle))
            {
                var FocusRect = Math2.CropCircle(rollIndicatorCicle, 10);
                var focus = data.Frame.SafeCopy(FocusRect);

                debugState.Add(focus);

                // Isolate the outside ring
                Mat maskInnerAlt = new Mat(focus.Size, DepthType.Cv8U, 3);
                maskInnerAlt.SetTo(new MCvScalar(1));
                CvInvoke.Circle(maskInnerAlt, new Point(focus.Size.Width / 2, focus.Size.Height / 2), (int)(rollIndicatorCicle.Radius - (rollIndicatorCicle.Radius * 0.2)), new Bgr(Color.White).MCvScalar, -1);
                CvInvoke.Circle(maskInnerAlt, new Point(focus.Size.Width / 2, focus.Size.Height / 2), (int)(rollIndicatorCicle.Radius - (rollIndicatorCicle.Radius * 0.275)), new Bgr(Color.Black).MCvScalar, -1);

                var outerMovingRingOnly = focus.Copy(maskInnerAlt.ToImage<Gray, byte>());
                var outerMovingRingWithoutBottom = outerMovingRingOnly.Copy(new Rectangle(0, 0, outerMovingRingOnly.Width, (int)(outerMovingRingOnly.Height))); // - (outerMovingRingOnly.Height * 0.29)
                var ring_hsv_unfiltered = outerMovingRingWithoutBottom.Convert<Hsv, byte>().InRange(new Hsv(20, 0, 85), new Hsv(180, 255, 255));
                var ring_hsv = Utils.RemoveBlobs(ring_hsv_unfiltered, 1, 500);

                debugState.Add(outerMovingRingOnly);
                debugState.Add(outerMovingRingWithoutBottom);
                debugState.Add(ring_hsv);
                debugState.Add(ring_hsv_unfiltered);

                return ReadRollAngleFromRingImage(ring_hsv, focus, debugState);
            }
            else
            {
                debugState.SetError($"ROLL: Main circles");
            }
            return double.NaN;
        }

        private double ReadRollAngleFromRingImage(Image<Gray,byte> img, Image<Bgr,byte> markupImage, DebugState debugState)
        {
            var circles = CvInvoke.HoughCircles(img, HoughType.Gradient, 2.0, 1.0, 1, 10, 50, 53);
            if (circles.Length > 0)
            {
                var cir = circles[0];
                var radius = cir.Radius;

                Func<int, int, bool> check = (int cX, int cY) =>
                {
                    if (cX < img.Width && cX > 0 &&
                    cY < img.Height && cY > 0)
                    {
                        var b = img[cY, cX];
                        return (b.Intensity > 0);
                    }
                    return false;
                };

                Func<int, int, bool> check2 = (int cX, int cY) =>
                {
                    return check(cX - 1, cY) ||
                           check(cX + 1, cY) ||
                           check(cX - 3, cY) ||
                           check(cX + 3, cY) ||
                           check(cX - 5, cY) ||
                           check(cX + 5, cY) ||
                           check(cX, cY + 2) ||
                           check(cX, cY - 2);
                };

                // From 6PM to 12PM clockwise
                PointF leftPoint = default(PointF);
                for (double t = Math.PI / 2; t < Math.PI * 1.5; t += 0.05f)
                {
                    var cX_ = (int)(radius * Math.Cos(t) + cir.Center.X);
                    var cY_ = (int)(radius * Math.Sin(t) + cir.Center.Y);

                    //CvInvoke.Circle(focus, new Point(cX_, cY_), 1, new Bgr(Color.Red).MCvScalar, 1);

                    if (check2(cX_, cY_))
                    {
                        leftPoint = new PointF(cX_, cY_);
                        break;
                    }
                }

                // From 6PM to 12PM counter-clockwise.
                PointF rightPoint = default(PointF);
                for (double t = Math.PI / 2; t > -0.5 * Math.PI; t -= 0.05f)
                {
                    var cX_ = (int)(radius * Math.Cos(t) + cir.Center.X);
                    var cY_ = (int)(radius * Math.Sin(t) + cir.Center.Y);

                    //CvInvoke.Circle(focus, new Point(cX_, cY_), 1, new Bgr(Color.Green).MCvScalar, 1);

                    if (check2(cX_, cY_))
                    {
                        rightPoint = new PointF(cX_, cY_);
                        break;
                    }
                }

                if (leftPoint != default(PointF) && rightPoint != default(PointF))
                {
                    CvInvoke.Line(markupImage, rightPoint.ToPoint(), leftPoint.ToPoint(), new Bgr(Color.Yellow).MCvScalar, 2);

                    var horizontalNeedleLine = new LineSegment2DF(leftPoint, rightPoint);

                    if (horizontalNeedleLine.Length < 88 || horizontalNeedleLine.Length > 140)
                    {
                        debugState.SetError($"ROLL: Dist {horizontalNeedleLine.Length}");
                        return double.NaN;
                    }

                    var angle = Math2.GetPolarHeadingFromLine(horizontalNeedleLine) - 270;
                    // skew considered from other panels
                    return angle + 1;
                }
                else
                {
                    debugState.SetError($"ROLL: Couldn't find boundary");
                }
            }
            else
            {
                debugState.SetError($"ROLL: boundary circles");
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

            if (data.Id > 0 &&
                Timeline.Data[data.Id - 1].Roll != null &&
                Timeline.Data[data.Id - 1].Roll.ForIndicatorUse != null)
            {
                // Our hint from last time.
                localRect = Math2.CropCircle((CircleF)Timeline.Data[data.Id - 1].Roll.ForIndicatorUse, 10);
            }

            // Crop and blur
            var cropped_frame = data.Frame.SafeCopy(localRect).PyrUp().PyrDown();

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
