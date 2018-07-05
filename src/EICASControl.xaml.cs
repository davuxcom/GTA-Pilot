using System;
using System.Windows;
using System.Windows.Controls;

namespace GTAPilot
{
    public partial class EICASControl : UserControl, ICanTick
    {
        public EICASControl()
        {
            InitializeComponent();

            App.Register(this);
        }

        public void Tick()
        {
            RightEngine.Height = Math.Max(10, ((FrameworkElement)RightEngine.Parent).ActualHeight * SystemManager.Instance.Computer.Thrust);
            LeftEngine.Height = Math.Max(10, ((FrameworkElement)LeftEngine.Parent).ActualHeight * SystemManager.Instance.Computer.Thrust);


            SetValue(txtEngineRight, SystemManager.Instance.Computer.Thrust * 100);
            SetValue(txtEngineLeft, SystemManager.Instance.Computer.Thrust * 100);

            SetValue(OILPLeft, SystemManager.Instance.IndicatorHost.Roll.Counter.Fps);
            SetValue(OILPRight, SystemManager.Instance.IndicatorHost.Pitch.Counter.Fps);
            SetValue(OILTLeft, SystemManager.Instance.IndicatorHost.Airspeed.Counter.Fps);
            SetValue(OILTRight, SystemManager.Instance.IndicatorHost.Altitude.Counter.Fps);
            SetValue(OILQLeft, SystemManager.Instance.IndicatorHost.Compass.Counter.Fps);
            SetValue(OILQRight, SystemManager.Instance.IndicatorHost.LandingGear.Counter.Fps);

            Flaps.Height = ((FrameworkElement)LeftEngine.Parent).ActualHeight * SystemManager.Instance.Computer.Flaps;

            GearDownPanel.Visibility = Timeline.Gear == 1 ? Visibility.Collapsed : Visibility.Visible;
            FlapsPanel.Visibility = SystemManager.Instance.Computer.Flaps > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetValue(TextBlock txt, double value)
        {
            txt.Text = "" + Math.Round(value);
        }
    }
}
