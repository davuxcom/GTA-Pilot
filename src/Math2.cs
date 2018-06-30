using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Linq;

namespace GTAPilot
{
    public class Math2
    {
        public static Rectangle CropCircle(CircleF circle, int margin)
        {
            int d = (int)circle.Radius * 2;
            int r = (int)circle.Radius;
            return new Rectangle((int)circle.Center.X - r - margin, (int)circle.Center.Y - r - margin, d + margin * 2, d + margin * 2);
        }

        public static double AddAngles(double a, double b)
        {
            var diff = ((a - b + 180 + 360) % 360) - 180;
            var angle = (360 + b + (diff / 2)) % 360;
            return angle;
        }

        public static double ToRad(double degree)
        {
            return (Math.PI / 180) * degree;
        }

        // TODO: Should fix/remove this mess, almost definitely only exists because we
        // have the wrong baseline.
        public static double FixAngle(double angle, Point center_point, Point other_point)
        {
            var v_angle = (angle * (180 / Math.PI));

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
            else
            {
                //  Trace.WriteLine("EXTA " + v_angle);
            }
            return v_angle;
        }


        public static double angleBetween2Lines(LineSegment2D line1, LineSegment2D line2)
        {
            double angle1 = Math.Atan2(line1.P1.Y - line1.P2.Y,
                                       line1.P1.X - line1.P2.X);
            double angle2 = Math.Atan2(line2.P1.Y - line2.P2.Y,
                                       line2.P1.X - line2.P2.X);
            return angle1 - angle2;
        }

        public static double angleBetween2Lines(LineSegment2DF line1, LineSegment2DF line2)
        {
            double angle1 = Math.Atan2(line1.P1.Y - line1.P2.Y,
                                       line1.P1.X - line1.P2.X);
            double angle2 = Math.Atan2(line2.P1.Y - line2.P2.Y,
                                       line2.P1.X - line2.P2.X);
            return angle1 - angle2;
        }

        internal static double GetPolarHeadingFromLine(LineSegment2DF targetLine)
        {
            return GetPolarHeadingFromLine(targetLine.P1, targetLine.P2);
        }

        // TODO: Same as ScaleValue elsewhere
        public static double MapValue(double a0, double a1, double b0, double b1, double a)
        {
            return b0 + (b1 - b0) * ((a - a0) / (a1 - a0));
        }

        // TODO: not clear if this returns positive angle delta only
        public static double DiffAngles(double a, double b)
        {
            var diff = ((a - b + 180 + 360) % 360) - 180;
            return diff;
        }



        public static double GetDistance(PointF point1, PointF point2)
        {
            // pythagorean theorem c^2 = a^2 + b^2
            // thus c = square root(a^2 + b^2)
            double a = (double)(point2.X - point1.X);
            double b = (double)(point2.Y - point1.Y);
            return Math.Sqrt(a * a + b * b);
        }

        public static double GetPolarHeadingFromLine(PointF pt1, PointF pt2)
        {
            var heading = Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X);
            // Starting at 9PM clockwise to 3PM: 0 to pi
            // Starting at 9PM counter-clockwise to 3PM: 0 to -pi
            if (heading >= 0)
            {
                // 0 to 180
                heading = (heading * (180 / Math.PI));
            }
            else
            {
                // 0 to -180
                heading = heading * (180 / Math.PI);
                // 180 to 0
                heading = 180 + heading;
                // 360 to 180
                heading = heading + 180;
            }

            // Skew so 0 is at 12PM.
            heading -= 90;
            if (heading < 0) heading = 360 + heading;

            return heading;
        }

        public static double GetDistanceFromLine(PointF pt, LineSegment2DF line)
        {
            var a = line.P1;
            var c = line.P2;
            var b = pt;

            // normalize points
            var cn = new PointF(c.X - a.X, c.Y - a.Y);
            var bn = new PointF(b.X - a.X, b.Y - a.Y);

            double angle = Math.Atan2(bn.Y, bn.X) - Math.Atan2(cn.Y, cn.X);
            double abLength = Math.Sqrt(bn.X * bn.X + bn.Y * bn.Y);

            return Math.Sin(angle) * abLength;
        }

        internal static double SafeAddAngle(double angle, double add)
        {
            angle += add;
            if (angle > 359) angle = angle - 360;
            if (angle < 0) angle = 360 + angle;
            return angle;
        }
    }

}
