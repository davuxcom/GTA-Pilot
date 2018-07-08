using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GTAPilot
{
    public partial class EICASControl : UserControl, ICanTick
    {
        public ObservableCollection<string> Warnings = new ObservableCollection<string>();

        Dictionary<string, Func<TimelineFrame, double>> _warnings = new Dictionary<string, Func<TimelineFrame, double>>();

        public EICASControl()
        {
            InitializeComponent();

            App.Register(this);

            MasterCaution.ItemsSource = Warnings;

            _warnings.Add("NO ROLL DATA", f => f.Roll.Value);
            _warnings.Add("NO PITCH DATA", f => f.Pitch.Value);
            _warnings.Add("NO YAW DATA", f => f.Heading.Value);
            _warnings.Add("NO ALTITUDE DATA", f => f.Altitude.Value);
            _warnings.Add("NO SPEED DATA", f => f.Speed.Value);
            _warnings.Add("NO GEAR INDICATION", f => f.LandingGear.Value);

            // Other ideas:
            // FRIDA FAIL
            // LOW FRAME RATE
            // CAN'T KEEP UP
        }

        int ticks = 0;
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

            Flaps.Height = ((FrameworkElement)Flaps.Parent).ActualHeight * SystemManager.Instance.Computer.Flaps;

            GearDownPanel.Visibility = Timeline.Gear == 1 ? Visibility.Collapsed : Visibility.Visible;
            FlapsPanel.Visibility = SystemManager.Instance.Computer.Flaps > 0 ? Visibility.Visible : Visibility.Collapsed;

            ticks++;
            if (ticks % 10 == 0)
            {
                foreach (var kp in _warnings)
                {
                    var lastValue = Timeline.LatestFrame(kp.Value, Timeline.LatestFrameId);
                    if (lastValue != null)
                    {
                        var dt = Timeline.Duration.Elapsed.TotalSeconds - lastValue.Seconds;
                        if (dt > 1)
                        {
                            if (!Warnings.Contains(kp.Key))
                            {
                                Warnings.Add(kp.Key);
                            }
                        }
                        else
                        {
                            if (Warnings.Contains(kp.Key))
                            {
                                Warnings.Remove(kp.Key);
                            }
                        }
                    }
                    else
                    {
                        if (!Warnings.Contains(kp.Key))
                        {
                            Warnings.Add(kp.Key);
                        }
                    }
                }

            }
        }

        private void SetValue(TextBlock txt, double value)
        {
            txt.Text = "" + Math.Round(value);
        }
    }
}
