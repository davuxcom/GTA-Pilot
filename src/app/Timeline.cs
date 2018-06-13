using System;

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
        public double Roll = double.NaN;
        public double Pitch = double.NaN;
        public double Altitude = double.NaN;

        public CompassExtendedFrame Extended = new CompassExtendedFrame();

      //  public PointF UpdatedLocation;
      //  public DateTime UpdateTime;
        
    }

    class Timeline
    {
        public static int LastFrameId;

        // TODO: static to save allocations but bad
        public static TimelineFrame[] Data = new TimelineFrame[90000];

        public static double Roll => Latest(f => f.Roll);
        public static double Pitch => Latest(f => f.Pitch);
        public static double Speed => Latest(f => f.Speed);
        public static double Altitude => Latest(f => f.Altitude);
        public static double Heading => Latest(f => f.Heading);


        private static double Latest(Func<TimelineFrame, double> finder)
        {
            for(var i = LastFrameId; i >= 0; i--)
            {
                if (Data[i] != null)
                {
                    if (!double.IsNaN(finder(Data[i])))
                    {
                        return finder(Data[i]);
                    }
                }
            }
            return double.NaN;
        }
    }
}
