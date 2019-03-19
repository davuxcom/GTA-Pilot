using Emgu.CV.Structure;
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
        public static double Gear => LatestAvg(1, f => f.LandingGear.Value, LatestFrameId);

        public static double RollAvg => LatestAvg(25, f => f.Roll.Value, LatestFrameId);
        public static double PitchAvg => LatestAvg(25, f => f.Pitch.Value, LatestFrameId);
        public static double AltitudeAvg => LatestAvg(25, f => f.Altitude.Value, LatestFrameId);
        public static double SpeedAvg => LatestAvg(25, f => f.Speed.Value, LatestFrameId);

        public static bool IsInGame { get; set; }

        public static void Begin()
        {
            Duration = Stopwatch.StartNew();
            Resume();
        }

        public static void Resume()
        {
            StartLocationThread();
        }

        private static void StartLocationThread()
        {
            var t = new Thread(() =>
            {
                int lastDoneFrame = LatestFrameId - 1;

                IsInGame = true;
                while (IsInGame)
                {
                    for (var i = lastDoneFrame + 1; i <= LatestFrameId; i++)
                    {
                        if (Data[i] != null && Data[i].IsDataComplete)
                        {
                            CompleteFrame(i);
                            lastDoneFrame = i;

                            var dt = Duration.Elapsed.TotalSeconds - Data[i].Seconds;

                           // Trace.WriteLine("LOC: " + dt);
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

        public static double LatestAvg(int count, Func<TimelineFrame, double> finder, int startFrameId, bool useHeadingMath = false)
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

                var priorHdg = LatestAvg(1, f => f.Heading.Value, id - 1, useHeadingMath: true);
                var spd = LatestAvg(4, f => f.Speed.Value, id);

                var hdg = LatestAvg(spd > 100 ? 60 : 240, f => f.Heading.Value, id, useHeadingMath: true);

                var roll = LatestAvg(1, f => f.Roll.Value, id);
                if (!double.IsNaN(hdg) && !double.IsNaN(spd) && !double.IsNaN(roll))
                {
                    var rollValue = 0; // Math2.Clamp(roll * 0.09, -3, 3);

                    var dt = newFrame.Seconds - lastFrame.Seconds;
                    var positionDelta = ComputePositionChange(Math2.ClampAngle(hdg - rollValue), spd, dt);
                    newFrame.Location = lastFrame.Location.Add(positionDelta);
                    
                    /*
                   if (!double.IsNaN(priorHdg))
                   {
                        var derivative = (hdg - priorHdg) / dt;
                        var angle = 1 * Math.Sign(derivative) * 90;
                        var rollSkew = 0.2 * Math.Abs(derivative);

                       var side_delta = ComputePositionChange(Math2.SafeAddAngle(hdg, angle), rollSkew, dt);
                        var dist = Math2.GetDistance(default(PointF), side_delta);
                        if (dist < 0.3)
                        {
                            newFrame.Location = newFrame.Location.Add(side_delta);
                        }
                        else
                        {
                           // Trace.WriteLine("TL: SIDE: " + dist);
                        }
                   }
                   */
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

        public static void Reset()
        {
            for (var i = 0; i < LatestFrameId; i++)
            {
                Data[i] = null;
            }
            LatestFrameId = 0;
            CurrentLocation = StartLocation;
        }

        static int menuExitDelay = 0;

        public static void ResetGameFromSavePointByMenu()
        {
            IsInGame = false;

            new Thread(() =>
            {
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.START, 10);
                SystemManager.Instance.App.Controller.Flush();
                while (!SystemManager.Instance.IndicatorHost.Menu.IsInMenu)
                {
                    Thread.Sleep(100);
                }


                Thread.Sleep(800);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.A, 10);
                SystemManager.Instance.App.Controller.Flush();
                Thread.Sleep(1200);

                var location = SystemManager.Instance.IndicatorHost.Menu.Location;
                while (location == default(PointF))
                {
                    //      Trace.WriteLine("wait for location");
                    Thread.Sleep(400);
                    location = SystemManager.Instance.IndicatorHost.Menu.Location;
                }

                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.B, 10);
                SystemManager.Instance.App.Controller.Flush();
                Thread.Sleep(900);

                var line = new LineSegment2DF(Timeline.CurrentLocation, location);
                Trace.WriteLine($"MOVE: {Math.Round(line.Length)} {Math.Round(Math2.GetPolarHeadingFromLine(line))}");
                Trace.WriteLine($"Location: {location}");













                //   Trace.WriteLine("now in menu!");

                while (!SystemManager.Instance.IndicatorHost.Menu.SelectedMenuItem.Contains("GAME"))
                {
                 //   Trace.WriteLine("SELECTED: " + SystemManager.Instance.IndicatorHost.Menu.SelectedMenuItem);
                    SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.DPAD_LEFT, 10);
                    SystemManager.Instance.App.Controller.Flush();
                    Thread.Sleep(500);
                }

             //   Trace.WriteLine("now in game!");
                Thread.Sleep(200);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.A, 10);
                SystemManager.Instance.App.Controller.Flush();

              //  Trace.WriteLine("now game menu list");


                while (SystemManager.Instance.IndicatorHost.Menu.SelectedGameMenuItem != "LOADGAME")
                {
            //        Trace.WriteLine("SELECTED: " + SystemManager.Instance.IndicatorHost.Menu.SelectedGameMenuItem);
                    SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.DPAD_DOWN, 10);
                    SystemManager.Instance.App.Controller.Flush();

                    Thread.Sleep(500);
                }

              //  Trace.WriteLine("now save list");

                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.A, 10);
                SystemManager.Instance.App.Controller.Flush();
                Thread.Sleep(800);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.A, 10);
                SystemManager.Instance.App.Controller.Flush();
                Thread.Sleep(800);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.A, 10);
                SystemManager.Instance.App.Controller.Flush();
                Thread.Sleep(800);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.A, 10);
                SystemManager.Instance.App.Controller.Flush();

             //   Trace.WriteLine("now game should be loading");

                while (!SystemManager.Instance.IndicatorHost.Loading.IsLoading)
                {
               //     Trace.WriteLine("wait for loading " + SystemManager.Instance.IndicatorHost.Loading.LoadingTextRead);

                    Thread.Sleep(1000);
                }

             //   Trace.WriteLine("confirm loading!");

                while (SystemManager.Instance.IndicatorHost.Loading.IsLoading)
                {
              //      Trace.WriteLine("wait for no loading " + SystemManager.Instance.IndicatorHost.Loading.LoadingTextRead);

                    Thread.Sleep(1000);
                }

                SystemManager.Instance.FlightPlan.CurrentIndex = 0;
                Reset();

                Trace.WriteLine("GAME READY!");
                Timeline.Begin();

                Thread.Sleep(2000);

                SystemManager.Instance.MCP.IAS = 120;
                SystemManager.Instance.MCP.ALT = 1200;
                SystemManager.Instance.MCP.AltitudeHold = true;
                SystemManager.Instance.MCP.LNAV = true;
                SystemManager.Instance.MCP.IASHold = true;

            }).Start();
        }

        public static void UpdateLocationFromMenu()
        {
            IsInGame = false;
            new Thread(() =>
            {
                //Trace.WriteLine("EnterMenu");

                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.RIGHT_SHOULDER, 0);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.LEFT_SHOULDER, 0);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.START, 12);
                SystemManager.Instance.App.Controller.Flush();
                while (!SystemManager.Instance.IndicatorHost.Menu.IsInMenu)
                {
                    Thread.Sleep(100);
                }

             //   Trace.WriteLine("now in menu!");
                Thread.Sleep(800);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.A, 12);
                SystemManager.Instance.App.Controller.Flush();
                Thread.Sleep(1200);
              //  Trace.WriteLine("now in selected map");

                var location = SystemManager.Instance.IndicatorHost.Menu.Location;
                while (location == default(PointF))
                {
              //      Trace.WriteLine("wait for location");
                    Thread.Sleep(400);
                    location = SystemManager.Instance.IndicatorHost.Menu.Location;
                }
                Thread.Sleep(700);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.B, 18);
                SystemManager.Instance.App.Controller.Flush();
                Thread.Sleep(1000);
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.B, 18);
                SystemManager.Instance.App.Controller.Flush();

                var final_sleep = (int)(18*(1000/SystemManager.Instance.App.Controller.XInput_In.Fps));

                var line = new LineSegment2DF(Timeline.CurrentLocation, location);
                Trace.WriteLine($"MOVE: {Math.Round(line.Length)}");
                Trace.WriteLine($"Location: {location} sleep={final_sleep}");


                CurrentLocation = location;
                for (var i = 1; i < 5; i++)
                {

                    Data[LatestFrameId - i].Seconds = Duration.Elapsed.Add(TimeSpan.FromMilliseconds(final_sleep)).TotalSeconds;
                    Data[LatestFrameId - i].Location = CurrentLocation;
                    Data[LatestFrameId - i].Roll.Value = RollAvg;
                    Data[LatestFrameId - i].Pitch.Value = PitchAvg;
                    Data[LatestFrameId - i].IsLocationCalculated = true;
                    Data[LatestFrameId - i].IsResetLocation = true;
                    Data[LatestFrameId - i].IsDataComplete = true;
                }

                while (Duration.Elapsed.TotalSeconds < Data[LatestFrameId - 1].Seconds)
               {
                   Thread.Sleep(1); 
                }

                //   Thread.Sleep(final_sleep);
                // 500 is pretty ok!

                SystemManager.Instance.Computer._rollPid.ClearError();
                SystemManager.Instance.Computer._pitchPid.ClearError();



                Resume();
            }).Start();
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
