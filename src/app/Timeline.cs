using GTAPilot.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

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
        public double Seconds;

        public double Heading = double.NaN;
        public double Speed = double.NaN;
        public double Roll = double.NaN;
        public double Pitch = double.NaN;
        public double Altitude = double.NaN;

        public CompassExtendedFrame Extended = new CompassExtendedFrame();

        public PointF Location;
        internal bool IsComplete;
        internal bool LocationComplete;
    }

    class Timeline
    {
        public static int LastFrameId;

        // TODO: static to save allocations but bad
        public static TimelineFrame[] Data = new TimelineFrame[90000];

        public static PointF StartLocation = new PointF(2030.2f, 4573.9f);
        public static PointF CurrentLocation;

        public static double Roll => Latest(f => f.Roll);
        public static double Pitch => Latest(f => f.Pitch);
        public static double Speed => Latest(f => f.Speed);
        public static double Altitude => Latest(f => f.Altitude);
        public static double Heading => Latest(f => f.Heading);

        public static void StartPositionThread()
        {
            new Thread(() =>
            {
                int lastDoneFrame = -1;

                while (true)
                {
                    for (var i = lastDoneFrame + 1; i <= LastFrameId; i++)
                    {
                        if (Data[i] != null && Data[i].IsComplete)
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
            }).Start();
        }

        private static double Latest(Func<TimelineFrame, double> finder)
        {
            for (var i = LastFrameId; i >= 0; i--)
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

        private static TimelineFrame LatestFrame(Func<TimelineFrame, double> finder, int endId)
        {
            for (var i = endId; i >= 0; i--)
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
                newFrame.LocationComplete = true;
                newFrame.Location = StartLocation;
            }
            else
            {
                TimelineFrame oldFrame;
                if (!double.IsNaN(newFrame.Heading) && !double.IsNaN(newFrame.Speed))
                {
                    // Find a frame with valid speed and heading
                    oldFrame = LatestFrame((f) => f.Heading + f.Speed, id - 1);
                    if (oldFrame != null && oldFrame != newFrame)
                    {
                        var dx = ComputePositionChange(oldFrame, newFrame);
                        newFrame.Location = oldFrame.Location.Add(dx);
                        newFrame.LocationComplete = true;
                        CurrentLocation = newFrame.Location;

                        return;
                    }
                }

                oldFrame = LatestFrame((f) => f.Location == default(PointF) ? double.NaN : 0, id - 1);
                newFrame.Location = oldFrame.Location;
                newFrame.LocationComplete = true;
                CurrentLocation = newFrame.Location;
            }
            
        }

        private static PointF ComputePositionChange(TimelineFrame oldFrame, TimelineFrame newFrame)
        {
            var timeDelta = newFrame.Seconds - oldFrame.Seconds;

            var speed = newFrame.Speed;

            var knotsPerHour = speed;
            var KnotsPerSecondToMetersPerSecond = 0.51444444444;
            var MetersPerSecond = knotsPerHour * KnotsPerSecondToMetersPerSecond;
            var scale = 1 / 3.32;

            double heading = newFrame.Heading;

            if (!double.IsNaN(newFrame.Extended.Bias) &&
                !double.IsNaN(newFrame.Extended.RollBias))
            {
                heading = heading - newFrame.Extended.Bias + (newFrame.Extended.RollBias + 1.4);
            }

            return new PointF((float)(Math.Sin(Math2.ToRad(heading)) * (scale * MetersPerSecond * timeDelta)),
                              (float)(Math.Cos(Math2.ToRad(heading)) * (scale * MetersPerSecond * timeDelta * -1)));
        }
    }
}
