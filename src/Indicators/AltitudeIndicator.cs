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
    class AltitudeIndicator : ISimpleIndicator
    {
        public double CachedTuningValue => dyn_lower.CachedValue;
        public double LastGoodValue => Timeline.Altitude;

        DynHsv dyn_lower = new DynHsv(0, 0, double.NaN, 0.008, 100);

        private static bool TryFindCircleInFullFrame(IndicatorData data, out CircleF ret)
        {
            ret = default(CircleF);
            if (RollIndicator.TryFindRollCircleInFullFrame(data, out var circle))
            {
                circle.Center = new PointF(circle.Center.X + 140, circle.Center.Y - 40);
                circle.Radius = 45;
                var firstCrop = Math2.CropCircle(circle, 25);
                var focus = data.Frame.SafeCopy(firstCrop);

                var vs_hsv = focus.Convert<Hsv, byte>().PyrUp().PyrDown();

                var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 80, 4, 50, 40, 50);
                if (circles.Length == 1)
                {
                    circles[0].Center = circles[0].Center.Add(firstCrop.Location);
                    circles[0].Radius = 45;
                    ret = circles[0];
                    return true;
                }
            }
            return false;
        }

        private Rectangle GetCenterBoxFromImage(Image<Bgr, byte> focus)
        {
            var center_size = 35;
            var center_box_point = new Point((focus.Width / 2) - (center_size / 2), (focus.Height / 2) - (center_size / 2));
            return new Rectangle(center_box_point, new Size(center_size, center_size));
        }

        public double ReadValue(IndicatorData data, DebugState debugState)
        {
            if (TryFindCircleInFullFrame(data, out var circle))
            {
                var focus = data.Frame.SafeCopy(Math2.CropCircle(circle, 15));
                var vs_blackimg = focus.Convert<Hsv, byte>().DynLowInRange(dyn_lower, new Hsv(180, 255, 255)).PyrUp().PyrDown();
                var markedup_frame = vs_blackimg.Convert<Bgr, byte>();
                debugState.Add(focus);
                debugState.Add(vs_blackimg);
                debugState.Add(markedup_frame);

                var cannyEdges3 = new Mat();
                CvInvoke.Canny(vs_blackimg, cannyEdges3, 10, 120);
                Mat dialatedCanny = new Mat();
                CvInvoke.Dilate(cannyEdges3, dialatedCanny, null, new Point(-1, -1), 1, BorderType.Default, new Gray(0).MCvScalar);

                Rectangle center = GetCenterBoxFromImage(focus);
                CvInvoke.Rectangle(markedup_frame, center, new Bgr(Color.Red).MCvScalar, 1);

                var lines = CvInvoke.HoughLinesP(dialatedCanny, 1, Math.PI / 45.0, 20, 16, 0).OrderByDescending(l => l.Length);
                foreach (var line in lines)
                {
                    CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);

                    if (center.Contains(line.P1) || center.Contains(line.P2))
                    {
                        LineSegment2D needleLine;
                        if (center.Contains(line.P1))
                        {
                            needleLine = new LineSegment2D(line.P2, line.P1);
                        }
                        else
                        {
                            needleLine = new LineSegment2D(line.P1, line.P2);
                        }

                        var angle = Math2.GetPolarHeadingFromLine(needleLine);
                        var hundreds = Math.Round((angle / 360) * 999);

                        CvInvoke.Line(focus, needleLine.P1, needleLine.P2, new Bgr(Color.Yellow).MCvScalar, 2);

                        var candidates = new List<double>();
                        for (var i = 0; i < 9; i++)
                        {
                            candidates.Add(hundreds + (i * 1000));
                        }

                        var ret = candidates.OrderBy(c => Math.Abs(c - (double.IsNaN(Timeline.Altitude) ? -200 : Timeline.Altitude))).First();

                        if (!double.IsNaN(Timeline.Altitude))
                        {
                            var delta = Math.Abs(ret - Timeline.Altitude);
                            if (delta > 400)
                            {
                                debugState.SetError($"Bad value {delta} > 400");
                                return double.NaN;
                            }
                        }

                        return ret;
                    }
                }
            }
            return double.NaN;
        }
    }
}