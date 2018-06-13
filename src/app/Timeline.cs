using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot
{
    public class CompassExtendedFrame
    {
        public double LastN = double.NaN;
        public double LastE = double.NaN;
        public double LastS = double.NaN;
        public double LastW = double.NaN;
        public double Bias = double.NaN;
        public double RollBias = double.NaN;
    }

    public class TimelineFrame
    {
        public long Id;
        public DateTime Time;

        public double Heading = double.NaN;
        public double Speed = double.NaN;
        public double Speed2 = double.NaN;
        public double Roll = double.NaN;
        public double Pitch = double.NaN;
        public double Altitude = double.NaN;

        public CompassExtendedFrame Extended = new CompassExtendedFrame();

        public PointF UpdatedLocation;
        public DateTime UpdateTime;
        
    }

    class Timeline
    {
        // TODO: static to save allocations but bad
        public static TimelineFrame[] Data = new TimelineFrame[90000];

    }
}
