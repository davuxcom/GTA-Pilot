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
        public double CachedTuningValue => _dyn_lower_only_needle.CachedValue;
        public double LastGoodValue => Timeline.Speed;

        private const int VALUE_DELTA_MAX = 40;
        private int num_rejected_values = 0;
        private DynHsv _dyn_lower_only_needle = new DynHsv(0, 0, double.NaN, 0.005, 100);

        public double ReadValue(IndicatorData data, DebugState debugState)
        {
            if (TryFindCircleInFullFrame(data, out var circle))
            {
                var focus = data.Frame.Copy(Math2.CropCircle(circle, 10));
                debugState.Add(focus);

                List<double> ret = new List<double>();
                foreach (var line in GetLinesFromFocusImage(focus, circle, debugState))
                {
                    CvInvoke.Line(focus, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 2);

                    var knots = ReadKnotsFromNeedleLine(line);

                    if (IsValueInExpectedRange(knots)) ret.Add(knots);
                }

                return CalculateResultFromAllResults(ret, debugState);
            }
            else
            {
                debugState.SetError("No circles");
            }
            return double.NaN;
        }

        private double ReadKnotsFromNeedleLine(LineSegment2D line)
        {
            var polarAngleOfNeedle = Math2.GetPolarHeadingFromLine(line) - 2; // Skew angle.

            // 0-360 deg -> 0-180 kt
            var knots = (polarAngleOfNeedle / 2);
            if (knots > 175) knots = 0;
            if (knots < 0) knots = 0;
            return knots;
        }

        private double CalculateResultFromAllResults(List<double> ret, DebugState debugState)
        {
            if (ret.Count == 0)
            {
                debugState.SetError("zero result count");
                return double.NaN;
            }

            ret.Sort();

            var finalKnots = ret.Average();
            if (ret.Count > 3) finalKnots = ret[ret.Count / 2];

            if (!IsValueInExpectedRange(finalKnots) && num_rejected_values < 10)
            {
                num_rejected_values++;
                debugState.SetError($"REJECT: kt={finalKnots} lastValue={Timeline.Speed}");
                return double.NaN;
            }

            num_rejected_values = 0;
            return finalKnots;
        }

        private bool IsValueInExpectedRange(double value)
        {
            return double.IsNaN(Timeline.Speed) || Math.Abs(value - Timeline.Speed) <= VALUE_DELTA_MAX;
        }

        private Rectangle GetCenterBoxFromImage(Image<Bgr, byte> focus)
        {
            var center_size = 40;
            var center_box_point = new Point((focus.Width / 2) - (center_size / 2), (focus.Height / 2) - (center_size / 2));
            return new Rectangle(center_box_point, new Size(center_size, center_size));
        }

        private IEnumerable<LineSegment2D> GetLinesFromFocusImage(Image<Bgr, byte> focus, CircleF circle, DebugState debugState)
        {
            Mat vspeedMask = new Mat(focus.Size, DepthType.Cv8U, 3);
            vspeedMask.SetTo(new MCvScalar(1));
            CvInvoke.Circle(vspeedMask, new Point(focus.Width / 2, focus.Height / 2), (int)(circle.Radius - 12), new Bgr(Color.White).MCvScalar, -1);

            var vspeed_inner_hsv = focus.Copy(vspeedMask.ToImage<Gray, byte>()).Convert<Hsv, byte>()
                .DynLowInRange(_dyn_lower_only_needle, new Hsv(180, 255, 255));
            var clean_inner_no_blobs = Utils.RemoveBlobs(vspeed_inner_hsv, 1, 10);
            var cannyEdges3 = new Mat();
            CvInvoke.Canny(clean_inner_no_blobs, cannyEdges3, 10, 250);

            Mat dialatedCanny = new Mat();
            CvInvoke.Dilate(cannyEdges3, dialatedCanny, null, new Point(-1, -1), 1, BorderType.Default, new Gray(0).MCvScalar);

            debugState.Add(vspeed_inner_hsv);
            debugState.Add(clean_inner_no_blobs);
            debugState.Add(cannyEdges3.ToImage<Gray, byte>());
            debugState.Add(dialatedCanny);

            var lines = CvInvoke.HoughLinesP(dialatedCanny, 1, Math.PI / 45, 30, 20, 1).Where(p => p.Length > 18);

            Rectangle centerBox = GetCenterBoxFromImage(focus);
            var linesInCenterImage = new List<LineSegment2D>();
            foreach (var line in lines)
            {
                if (centerBox.Contains(line.P1))
                {
                    linesInCenterImage.Add(new LineSegment2D(line.P2, line.P1));
                }
                else if (centerBox.Contains(line.P2))
                {
                    linesInCenterImage.Add(new LineSegment2D(line.P1, line.P2));
                }
            }

            return linesInCenterImage.OrderByDescending(l => l.Length);
        }

        private static bool TryFindCircleInFullFrame(IndicatorData data, out CircleF ret)
        {
            ret = default(CircleF);

            if (RollIndicator.TryFindRollCircleInFullFrame(data, out var circle))
            {
                circle.Center = new PointF(circle.Center.X + 140, circle.Center.Y + 70);
                circle.Radius = 55;
                var firstCrop = Math2.CropCircle(circle, 10);
                var focus = data.Frame.SafeCopy(firstCrop);

                var circles = CvInvoke.HoughCircles(focus.Convert<Hsv, byte>()[2], HoughType.Gradient, 2.0, 20, 10, 180, 45, 55);
                if (circles.Length == 1)
                {
                    circles[0].Center = circles[0].Center.Add(firstCrop.Location);
                    circles[0].Radius = 50;
                    ret = circles[0];
                    return true;
                }
            }
            return false;
        }
    }
}