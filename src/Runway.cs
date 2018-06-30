using GTAPilot.Extensions;
using System.Drawing;

namespace GTAPilot
{
    class Runway
    {
        public PointF StartPoint { get; }
        public PointF EndPoint { get; }
        public double Elevation { get; }

        public double OppositeHeading => Math2.GetPolarHeadingFromLine(StartPoint, EndPoint);
        public double Heading => Math2.GetPolarHeadingFromLine(EndPoint, StartPoint);

        public double Length => Math2.GetDistance(StartPoint, EndPoint);

        public Runway(PointF startPoint, PointF endPoint, double elevation)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Elevation = elevation;
        }

        public PointF ExtendForward(double length)
        {
            return EndPoint.ExtendAlongHeading(Heading, length);
        }

        public PointF ExtendBackward(double length)
        {
            return StartPoint.ExtendAlongHeading(OppositeHeading, length);
        }
    }
}
