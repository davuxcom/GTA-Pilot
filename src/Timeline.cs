using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace GTAPilot
{
    class Timeline
    {
        public static int LatestFrameId;

        // 950000 gives us a max runtime of 4.2hrs at 60fps.
        public static TimelineFrame[] Data = new TimelineFrame[950000];
        public static PointF CurrentLocation;
        public static Stopwatch Duration;

        // TODO: We need a way to set location, this is an arbitrary limitation right now.
        // location is los santos runway 3
        public static PointF StartLocation = new PointF(2030.2f, 4573.9f);

        public static double Roll => LatestAvg(1, f => f.Roll.Value);
        public static double Pitch => LatestAvg(1, f => f.Pitch.Value);
        public static double Speed => LatestAvg(1, f => f.Speed.Value);
        public static double Altitude => LatestAvg(1, f => f.Altitude.Value);
        public static double Heading => LatestAvg(1, f => f.Heading.Value);

        public static double RollAvg => LatestAvg(25, f => f.Roll.Value);
        public static double PitchAvg => LatestAvg(25, f => f.Pitch.Value);

        public static void Begin()
        {
            Duration = Stopwatch.StartNew();

            var t = new Thread(() =>
            {
                int lastDoneFrame = -1;

                while (true)
                {
                    for (var i = lastDoneFrame + 1; i <= LatestFrameId; i++)
                    {
                        if (Data[i] != null && Data[i].IsDataComplete)
                        {
                            CompleteFrame(i);
                            lastDoneFrame = i;
                        }
                        else
                        {
                            break; // bail on first non-complete frame, try next time
                        }
                    }

                    Thread.Sleep(10);
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private static double LatestAvg(int count, Func<TimelineFrame, double> finder)
        {
            List<double> ret = new List<double>();

            for (var i = LatestFrameId; i >= 0; i--)
            {
                if (Data[i] != null)
                {
                    if (!double.IsNaN(finder(Data[i])))
                    {
                        ret.Add(finder(Data[i]));

                        if (ret.Count == count) break;
                    }
                }
            }

            if (ret.Count == 0) return 0;

            return ret.Sum() / ret.Count;
        }

        public static TimelineFrame LatestFrame(Func<TimelineFrame, double> finder, int endId)
        {
            for (var i = endId - 1; i >= 0; i--)
            {
                if (Data[i] != null)
                {
                    if (!double.IsNaN(finder(Data[i])))
                    {
                        return Data[i];
                    }
                }
            }
            return null;
        }

        internal static void CompleteFrame(int id)
        {
            var newFrame = Data[id];
            if (id == 0)
            {
                newFrame.IsLocationCalculated = true;
                newFrame.Location = StartLocation;
            }
            else
            {
                var lastFrame = Data[id - 1];

                var hdg = LatestAvg(2, f => f.Heading.Value);
                var spd = LatestAvg(4, f => f.Speed.Value);

                if (!double.IsNaN(hdg) && !double.IsNaN(spd))
                {
                    var dx = ComputePositionChange(hdg, spd, newFrame.Seconds - lastFrame.Seconds);
                    newFrame.Location = lastFrame.Location.Add(dx);
                    newFrame.IsLocationCalculated = true;
                    CurrentLocation = newFrame.Location;
                }
                else
                {
                    // We don't have a Heading or Speed, so all we can do is copy forward.
                    newFrame.Location = lastFrame.Location;
                    newFrame.IsLocationCalculated = true;
                }
            }
        }

        private static PointF ComputePositionChange(double newHeading, double newSpeed, double dT)
        {
            var timeDeltaInSeconds = dT;
            double speedInKnotsPerHour = newSpeed;
            const double KnotsPerSecondToMetersPerSecond = 0.51444444444;
            double MetersPerSecond = speedInKnotsPerHour * KnotsPerSecondToMetersPerSecond;

            return new PointF((float)(Math.Sin(Math2.ToRad(newHeading)) * (Metrics.SCALE_METERS_TO_MAP4 * MetersPerSecond * timeDeltaInSeconds)),
                              (float)(Math.Cos(Math2.ToRad(newHeading)) * (Metrics.SCALE_METERS_TO_MAP4 * MetersPerSecond * timeDeltaInSeconds * -1)));
        }
    }
}
