using System.Drawing;

namespace GTAPilot
{
    public class TimelineFrame
    {
        public int Id;
        public double Seconds;

        public TimelineValue Heading = new TimelineValue();
        public TimelineValue Speed = new TimelineValue();
        public TimelineValue Roll = new TimelineValue();
        public TimelineValue Pitch = new TimelineValue();
        public TimelineValue Altitude = new TimelineValue();

        public PointF Location;
        public bool IsDataComplete;
        public bool IsLocationCalculated;
    }
}
