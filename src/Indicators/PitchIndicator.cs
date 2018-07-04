using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
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
            if (TryFindCircleInFullFrame(data, out var circle))
            {
                var focus = data.Frame.Copy(Math2.CropCircle(circle, 15));
                var vs_blackimg = focus.Convert<Hsv, byte>().DynLowInRange(dyn_lower, new Hsv(180, 255, 255));

                debugState.Add(focus);
                debugState.Add(vs_blackimg);

                int margin = 10;
                var vspeedMask = new Mat(focus.Size, DepthType.Cv8U, 3);
                vspeedMask.SetTo(new MCvScalar(1));
                CvInvoke.Circle(vspeedMask, Point.Round(new PointF(circle.Radius + margin, circle.Radius + margin)), (int)(circle.Radius - (circle.Radius * 0.1)), new Bgr(Color.White).MCvScalar, -1);
                vs_blackimg = vs_blackimg.Copy(vspeedMask.ToImage<Gray, byte>());

                var vspeed_inner_only = vs_blackimg.Copy(new Rectangle(0, 0, vs_blackimg.Width / 2, vs_blackimg.Height));

                debugState.Add(vspeed_inner_only);

                Rectangle center = GetCenterBoxFromImage(focus);
                foreach (var line in CvInvoke.HoughLinesP(vspeed_inner_only, 1, Math.PI / 45.0, 20, 20, 14))
                {
                    if (center.Contains(line.P1) || center.Contains(line.P2))
                    {
                        CvInvoke.Line(focus, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 2);

                        LineSegment2D needleLine;
                        if (center.Contains(line.P1))
                        {
                            needleLine = new LineSegment2D(line.P2, line.P1);
                        }
                        else
                        {
                            needleLine = new LineSegment2D(line.P1, line.P2);
                        }

                        var angle = (Math2.GetPolarHeadingFromLine(needleLine) - 270);
                        // bias up to account for skew
                        angle += 2.75;

                        if (line.Length > 63)
                        {
                            debugState.SetError($"Rejected length: {line.Length}");
                        }
                        return angle;
                    }
                }
            }
            return double.NaN;
        }

        private static bool TryFindCircleInFullFrame(IndicatorData data, out CircleF ret)
        {
            ret = default(CircleF);
            if (RollIndicator.TryFindRollCircleInFullFrame(data, out var circle))
            {
                circle.Center = new PointF(circle.Center.X, circle.Center.Y + 160);
                circle.Radius = 64;
                var firstCrop = Math2.CropCircle(circle, 25);
                var focus = data.Frame.SafeCopy(firstCrop);

                var vs_hsv = focus.Convert<Hsv, byte>().PyrUp().PyrDown();

                var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 80, 10, 180, 60, 80);
                if (circles.Length == 1)
                {
                    circles[0].Center = circles[0].Center.Add(firstCrop.Location);
                    circles[0].Radius = 64;
                    ret = circles[0];
                    return true;
                }
            }
            return false;
        }

        private Rectangle GetCenterBoxFromImage(Image<Bgr, byte> focus)
        {
            var center_size = 20;
            var center_box_point = new Point((focus.Width / 2) - (center_size / 2), (focus.Height / 2) - (center_size / 2));
            return new Rectangle(center_box_point, new Size(center_size, center_size));
        }
    }
}
