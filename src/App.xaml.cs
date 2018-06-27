using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace GTAPilot
{
    public partial class App : Application
    {
        private List<ICanTick> _ticks = new List<ICanTick>();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            new SystemManager();

            if (SystemManager.Instance.App.IsRunning)
            {
                new ImmersiveWindow().Show();
            }
            else
            {
                new AnalyzerWindow().Show();
            }

            var t = new Thread(TickThreadProc);
            t.IsBackground = true;
            t.Start();
        }

        internal static void Register(ICanTick tick) => ((App)App.Current)._ticks.Add(tick);

        private void TickThreadProc()
        {
            var w = Stopwatch.StartNew();
            while (true)
            {
                var start = w.Elapsed.TotalMilliseconds;
                // We do this with a background thread and synchronously in order to avoid queue starvation
                // on the dispatcher. Bindings were observed to be unserviced indefinitely.
                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var t in _ticks)
                    {
                        t.Tick();
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);

                var dt = w.Elapsed.TotalMilliseconds - start;
                Thread.Sleep(Math.Max(1, (int)(dt - (1000 / 30))));
            }
        }
    }
}
