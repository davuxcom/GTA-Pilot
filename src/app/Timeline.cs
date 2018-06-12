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

        public double this[InputType index]
        {
            get
            {
                switch (index)
                {
                    case InputType.Pitch: return Pitch;
                    case InputType.Roll: return Roll;
                    case InputType.Yaw: return Heading;
                    case InputType.Altitude: return Altitude;
                    case InputType.Speed: return Speed;
                }
                return 0;
            }
            set
            {
                switch ((InputType)index)
                {
                    case InputType.Pitch: Pitch = value; break;
                    case InputType.Roll: Roll = value; break;
                    case InputType.Yaw: Heading = value; break;
                    case InputType.Altitude: Altitude = value; break;
                    case InputType.Speed: Speed = value; break;
                }
            }
        }
    }

    class Timeline
    {
        // TODO: static to save allocations but bad
        public static TimelineFrame[] Data = new TimelineFrame[90000];

    }
}
