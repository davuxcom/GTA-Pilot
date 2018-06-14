using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GTAPilot
{

    public enum IndicatorChartType
    {
        Value, Delay
    }

    public class IndicatorChart : Canvas
    {
        public IndicatorViewModel Indicator
        {
            get { return (IndicatorViewModel)this.GetValue(IndicatorProperty); }
            set { this.SetValue(IndicatorProperty, value); }
        }
        public static readonly DependencyProperty IndicatorProperty = DependencyProperty.Register(
          "Indicator", typeof(IndicatorViewModel), typeof(IndicatorChart), new PropertyMetadata());

        public IndicatorChartType Type
        {
            get { return (IndicatorChartType)this.GetValue(TypeProperty); }
            set { this.SetValue(TypeProperty, value); }
        }
        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
          "Type", typeof(IndicatorChartType), typeof(IndicatorChart), new PropertyMetadata());


        int NUM_FRAMES = 120;
        private static DispatcherTimer _tickTimer;

        public IndicatorChart()
        {
            if (_tickTimer == null)
            {
                _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000 / 60) };
                _tickTimer.Start();
            }

            for (var i = 0; i < NUM_FRAMES; i++)
            {
                var l = new Line();
                l.Stroke = Brushes.Blue;
                l.StrokeThickness = 2;
                Children.Add(l);
            }

            _tickTimer.Tick += TickTimer_Tick;
        }

        private double GetValueForIndicator(TimelineFrame frame)
        {
            if (Type == IndicatorChartType.Delay)
            {
                switch (Indicator.Type)
                {
                    case IndicatorType.Roll: return frame.SvcRoll;
                    case IndicatorType.Pitch: return frame.SvcPitch;
                    case IndicatorType.Speed: return frame.SvcSpeed;
                    case IndicatorType.Altitude: return frame.SvcAltitude;
                    case IndicatorType.Yaw: return frame.SvcHeading;
                    default: throw new NotImplementedException();
                }
            }
            else
            {
                switch (Indicator.Type)
                {
                    case IndicatorType.Roll: return frame.Roll;
                    case IndicatorType.Pitch: return frame.Pitch;
                    case IndicatorType.Speed: return frame.Speed;
                    case IndicatorType.Altitude: return frame.Altitude;
                    case IndicatorType.Yaw: return frame.Heading;
                    default: throw new NotImplementedException();
                }
            }
        }

        private double[] GetRangeForIndicator()
        {
            if (Type == IndicatorChartType.Delay)
            {
                return new double[] { 0, 1 }; // seconds
            }
            else
            {
                switch (Indicator.Type)
                {
                    case IndicatorType.Roll: return new double[] { -30, 30 };
                    case IndicatorType.Pitch: return new double[] { -90, 90 };
                    case IndicatorType.Speed: return new double[] { 0, 180 };
                    case IndicatorType.Altitude: return new double[] { 0, 8500 };
                    case IndicatorType.Yaw: return new double[] { 1, 360 };
                    default: throw new NotImplementedException();
                }
            }
        }

        private void TickTimer_Tick(object sender, EventArgs e)
        {
            if (Indicator != null)
            {
                double current_x = Width;
                double x_size = Width / NUM_FRAMES;

                int childIndex = 0;
                TimelineFrame last = null;
                for (var i = Timeline.LastFrameId; i >= 0 && i > Timeline.LastFrameId - NUM_FRAMES; i--)
                {
                    var current = Timeline.Data[i];
                    if (last == null)
                    {
                        last = current;
                    }
                    else
                    {
                        var l = (Line)Children[childIndex];

                        if (!double.IsNaN(GetValueForIndicator(current)) && !double.IsNaN(GetValueForIndicator(last)))
                        {
                            l.X1 = current_x;
                            l.X2 = current_x - x_size;
                            l.Y1 = Math2.MapValue(GetRangeForIndicator()[0], GetRangeForIndicator()[1], Height, 0, GetValueForIndicator(current));
                            l.Y2 = Math2.MapValue(GetRangeForIndicator()[0], GetRangeForIndicator()[1], Height, 0, GetValueForIndicator(last));
                        }
                        else
                        {
                            l.X1 = l.X2 = l.Y1 = l.Y2 = 0;
                        }
                        current_x = current_x - x_size;
                        childIndex++;
                        last = current;
                    }

                }
            }
        }
    }
}
