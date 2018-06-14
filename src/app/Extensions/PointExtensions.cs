using System.Drawing;

namespace GTAPilot.Extensions
{
    public static class PointExtensions
    {
        public static Point ToPoint(this PointF pointf)
        {
            return new Point((int)pointf.X, (int)pointf.Y);
        }

        public static Point Add(this Point self, Point other)
        {
            return new Point(self.X + other.X, self.Y + other.Y);
        }

        public static PointF Add(this PointF self, Point other)
        {
            return new PointF(self.X + other.X, self.Y + other.Y);
        }

        public static PointF Add(this PointF self, PointF other)
        {
            return new PointF(self.X + other.X, self.Y + other.Y);
        }
    }
}
