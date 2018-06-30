using GTAPilot.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace GTAPilot
{
    public partial class App : Application
    {
        private Dictionary<System.Windows.Threading.Dispatcher, List<ICanTick>> _ticks = new Dictionary<System.Windows.Threading.Dispatcher, List<ICanTick>>();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            /*
            var a = Math2.GetPolarHeadingFromLine(new System.Drawing.PointF(
3624, 736), new System.Drawing.PointF(

3636, 857));
*/
/*
            
            FlightPlan fp = new FlightPlan();
            fp.LoadFromFile(@"c:\workspace\FlightPlan.txt");

            Timeline.Load(@"c:\workspace\run7.txt");

            var locationPoints = new List<System.Drawing.PointF>();
            for (var i = 0; i < Timeline.LatestFrameId; i++)
            {
                locationPoints.Add(Timeline.Data[i].Location);
            }

            new FlightPlanMap(fp.Points.ToArray(), locationPoints.ToArray()).Show();

            return;
            */
            new SystemManager();

            if (SystemManager.Instance.App.IsRunning)
            {
                new ImmersiveWindow().Show();
            }

            var t = new Thread(TickThreadProc);
            t.IsBackground = true;
            t.Start();

            t = new Thread(() =>
            {
                new AnalyzerWindow().Show();
                System.Windows.Threading.Dispatcher.Run();
            });
            t.IsBackground = true;
            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            /*
            t = new Thread(() =>
            {
                if (SystemManager.Instance.App.WindowHandle != IntPtr.Zero)
                {
                    User32.GetWindowRect(SystemManager.Instance.App.WindowHandle, out var rectNative);

                    bool running = true;
                    int ticks = 0;
                    while (true)
                    {
                        ticks++;

                        if (ticks % 100 == 0)
                        {
                            if (Keyboard.IsKeyDown(Key.LeftCtrl))
                            {
                                running = !running;
                            }
                        }

                        if (running)
                        {
                            var x = rectNative.right - 200;
                            var y = rectNative.top + 200;

                            x -= (ticks % 2 == 0) ? 100 : 0;
                            y += (ticks % 2 == 0) ? 100 : 0;

                            User32.SetCursorPos(x, y);
                        }


                        Thread.Sleep(2);

                       // if (ticks > 1000) Environment.Exit(0);
                    }
                }

            });
            t.IsBackground = true;
            t.SetApartmentState(ApartmentState.STA);

            t.Start();
            */
        }

        internal static void Register(ICanTick tick)
        {
            var ticks = ((App)App.Current)._ticks;

            if (!ticks.TryGetValue(System.Windows.Threading.Dispatcher.CurrentDispatcher, out var bucket))
            {
                ticks[System.Windows.Threading.Dispatcher.CurrentDispatcher] = new List<ICanTick>();
                bucket = ticks[System.Windows.Threading.Dispatcher.CurrentDispatcher];
            }

            bucket.Add(tick);
        }

        private void TickThreadProc()
        {
            var w = Stopwatch.StartNew();
            while (true)
            {
                var start = w.Elapsed.TotalMilliseconds;
                // We do this with a background thread and synchronously in order to avoid queue starvation
                // on the dispatcher. Bindings were observed to be unserviced indefinitely.

                foreach(var bucket in _ticks.Keys.ToArray())
                {
                    bucket.Invoke(() =>
                    {
                        foreach (var t in _ticks[bucket]) t.Tick();
                    });
                }

                var dt = w.Elapsed.TotalMilliseconds - start;
                Thread.Sleep(Math.Max(1, (int)((1000 / 10) - dt)));
            }
        }
    }
}
