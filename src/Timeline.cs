using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace GTAPilot
{
    public enum TimelineMode
    {
        NotConnected,
        Live,
        Menu,
        Loading,
        Disconnected
    }

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

        public static double Roll => LatestAvg(1, f => f.Roll.Value, LatestFrameId);
        public static double Pitch => LatestAvg(1, f => f.Pitch.Value, LatestFrameId);
        public static double Speed => LatestAvg(1, f => f.Speed.Value, LatestFrameId);
        public static double Altitude => LatestAvg(1, f => f.Altitude.Value, LatestFrameId);
        public static double Heading => LatestAvg(1, f => f.Heading.Value, LatestFrameId);

        public static double RollAvg => LatestAvg(25, f => f.Roll.Value, LatestFrameId);
        public static double PitchAvg => LatestAvg(25, f => f.Pitch.Value, LatestFrameId);
        public static double AltitudeAvg => LatestAvg(25, f => f.Altitude.Value, LatestFrameId);
        public static double SpeedAvg => LatestAvg(25, f => f.Speed.Value, LatestFrameId);

        public TimelineMode Mode { get; private set; }

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

                    Thread.Sleep(1);
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private static double LatestAvg(int count, Func<TimelineFrame, double> finder, int startFrameId, bool useHeadingMath = false)
        {
            List<double> ret = new List<double>();

            for (var i = startFrameId; i >= 0; i--)
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

            if (ret.Count == 0) return double.NaN;

            if (useHeadingMath)
            {
                if (ret.Count >= 8)
                {
                    var p1 = Math2.AddAngles(ret[0], ret[1]);
                    var p2 = Math2.AddAngles(ret[2], ret[3]);
                    var p3 = Math2.AddAngles(ret[4], ret[5]);
                    var p4 = Math2.AddAngles(ret[6], ret[7]);
                    var h1 = Math2.AddAngles(p1, p2);
                    var h2 = Math2.AddAngles(p3, p4);
                    return Math2.AddAngles(h1, h2);
                }
                else if (ret.Count >= 4)
                {
                    var p1 = Math2.AddAngles(ret[0], ret[1]);
                    var p2 = Math2.AddAngles(ret[2], ret[3]);
                    return Math2.AddAngles(p1, p2);
                }
                else return ret[0];
            }
            else
            {
                return ret.Sum() / ret.Count;
            }
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
                newFrame.Location = StartLocation;
            }
            else
            {
                var lastFrame = Data[id - 1];

                var hdg = LatestAvg(4, f => f.Heading.Value, id, useHeadingMath: true);
                var spd = LatestAvg(4, f => f.Speed.Value, id);
                var roll = LatestAvg(4, f => f.Roll.Value, id);
                if (!double.IsNaN(hdg) && !double.IsNaN(spd) && !double.IsNaN(roll))
                {
                    var dt = newFrame.Seconds - lastFrame.Seconds;
                    var positionDelta = ComputePositionChange(hdg, spd, dt);
                    newFrame.Location = lastFrame.Location.Add(positionDelta);
                
                    
                  // if (Math.Abs(roll) > 2 && Math.Abs(roll) < 10)
                   {
                       var angle = 1 * Math.Sign(roll) * 90;
                        var rollSkew = 0.5 * Math.Abs(roll);
                        var max_rollSkew = 4;

                        if (rollSkew > max_rollSkew) rollSkew = max_rollSkew;

                       var side_delta = ComputePositionChange(Math2.SafeAddAngle(hdg, angle), rollSkew, dt);
                    //   newFrame.Location = newFrame.Location.Add(side_delta);
                   }
                   

                    

                }
                else
                {
                    // We don't have a Heading or Speed, so all we can do is copy forward.
                    newFrame.Location = lastFrame.Location;
                }
            }

            newFrame.IsLocationCalculated = true;
            CurrentLocation = newFrame.Location;
        }

        public static void Save(string filePath)
        {
            var stopId = LatestFrameId;

            var ret = new List<string>();
            for (var i = 0; i < stopId; i++)
            {
                var f = Data[i];
                ret.Add($"{f.Seconds},{f.Roll.Value},{f.Pitch.Value},{f.Speed.Value},{f.Altitude.Value},{f.Heading.Value}");
            }

            System.IO.File.WriteAllLines(filePath, ret.ToArray());
        }

        public static void Load(string filePath)
        {
            var lines = System.IO.File.ReadAllLines(filePath);

            for(var i = 0; i < lines.Length; i++)
            {
                var d = Data[i] = new TimelineFrame { Id = i };

                var parts = lines[i].Split(',');
                d.Seconds = double.Parse(parts[0]);
                d.Roll.Value = double.Parse(parts[1]);
                d.Pitch.Value = double.Parse(parts[2]);
                d.Speed.Value = double.Parse(parts[3]);
                d.Altitude.Value = double.Parse(parts[4]);
                d.Heading.Value = Math2.SafeAddAngle(double.Parse(parts[5]), 0);
                CompleteFrame(i);
                LatestFrameId = i;
            }
        }

        public static void ResetFromSavePoint()
        {

        }

        public static void EnterMenu()
        {
            // TODO:
        }

        private static PointF ComputePositionChange(double newHeading, double speedInKnots, double timeDeltaInSeconds)
        {
            const double KnotsToMetersPerSecond = 0.51444444444;
            double MetersPerSecond = speedInKnots * KnotsToMetersPerSecond;

            return new PointF((float)(Math.Sin(Math2.ToRad(newHeading)) * (Metrics.SCALE_METERS_TO_MAP4 * MetersPerSecond * timeDeltaInSeconds)),
                              (float)(Math.Cos(Math2.ToRad(newHeading)) * (Metrics.SCALE_METERS_TO_MAP4 * MetersPerSecond * timeDeltaInSeconds * -1)));
        }
    }
}
