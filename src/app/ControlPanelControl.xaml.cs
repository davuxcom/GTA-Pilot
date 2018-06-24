using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class ControlPanelControl : UserControl
    {
        public ObservableCollection<IndicatorViewModel> Indicators { get; }

        DispatcherTimer _fpsTimer = new DispatcherTimer();

        public ControlPanelControl()
        {
            InitializeComponent();

            Indicators = new ObservableCollection<IndicatorViewModel>();
            Indicators.Add(new IndicatorViewModel("Roll", SystemManager.Instance.IndicatorHost.Roll));
            Indicators.Add(new IndicatorViewModel("Pitch", SystemManager.Instance.IndicatorHost.Pitch));
            Indicators.Add(new IndicatorViewModel("Speed", SystemManager.Instance.IndicatorHost.Airspeed));
            Indicators.Add(new IndicatorViewModel("Altitude", SystemManager.Instance.IndicatorHost.Altitude));
            Indicators.Add(new IndicatorViewModel("Yaw", SystemManager.Instance.IndicatorHost.Compass));

            DataContext = this;

            _fpsTimer.Interval = TimeSpan.FromMilliseconds(App.FPS);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            foreach (var i in Indicators) i.Tick();
        }
    }
}
