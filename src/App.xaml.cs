using System.Collections.Generic;
using System.Timers;
using System.Windows;

namespace GTAPilot
{
    public partial class App : Application
    {
        public static int FPS = 1000 / 30;

        private List<ICanTick> _ticks = new List<ICanTick>();
        private Timer _tickTimer = new Timer { AutoReset = true, Interval = FPS };

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _tickTimer.Elapsed += TickTimer_Elapsed;
            _tickTimer.Enabled = true;

            new ImmersiveWindow().Show();
        }

        internal static void Register(ICanTick tick) => ((App)App.Current)._ticks.Add(tick);

        private void TickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // We do this with a background thread and synchronously in order to avoid queue starvation
            // on the dispatcher. Bindings were observed to be unserviced indefinitely.
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var t in _ticks)
                {
                    t.Tick();
                }
            });
        }
    }
}
