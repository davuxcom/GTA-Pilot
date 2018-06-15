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
        Value, Delay, Output
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


        int NUM_FRAMES = 200 / 2; // even with Width, hack
        private static DispatcherTimer _tickTimer;

        public IndicatorChart()
        {
            if (_tickTimer == null)
            {
                _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(App.FPS) };
                _tickTimer.Start();
            }

            for (var i = 0; i < NUM_FRAMES; i++)
            {
                var l = new Line();
                l.Stroke = Brushes.Magenta;
                l.StrokeThickness = 1;
                Children.Add(l);
            }

            for (var i = 0; i < NUM_FRAMES; i++)
            {
                var l = new Line();
                l.StrokeThickness = 1;
                Children.Add(l);
            }

            _tickTimer.Tick += TickTimer_Tick;
        }

        private TimelineValue GetTimelineValueForIndicator(TimelineFrame frame)
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

        private double GetValueForIndicator(TimelineFrame frame)
        {
            if (Type == IndicatorChartType.Delay)
            {
                return GetTimelineValueForIndicator(frame).SecondsWhenComputed;
            }
            else if (Type == IndicatorChartType.Value)
            {
                return GetTimelineValueForIndicator(frame).Value;
            }
            else if (Type == IndicatorChartType.Output)
            {
                return GetTimelineValueForIndicator(frame).OutputValue;
            }
            throw new NotImplementedException();
        }

        private double GetSetPointForIndicator(TimelineFrame frame)
        {
            if (Type == IndicatorChartType.Value)
            {
                return GetTimelineValueForIndicator(frame).SetpointValue;
            }

            return double.NaN;
        }

        private double[] GetRangeForIndicator()
        {
            if (Type == IndicatorChartType.Delay)
            {
                return new double[] { 0, 1 }; // seconds
            }
            else if (Type == IndicatorChartType.Value)
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
            else if (Type == IndicatorChartType.Output)
            {
                switch (Indicator.Type)
                {
                    case IndicatorType.Roll: return new double[] { FlightComputerConfig.Roll.OV.Min, FlightComputerConfig.Roll.OV.Max };
                    case IndicatorType.Pitch: return new double[] { FlightComputerConfig.Pitch.OV.Min, FlightComputerConfig.Pitch.OV.Max };
                    case IndicatorType.Speed: return new double[] { FlightComputerConfig.Speed.OV.Min, FlightComputerConfig.Speed.OV.Max };
                    case IndicatorType.Altitude: return new double[] { -1, 1 };
                    case IndicatorType.Yaw: return new double[] { FlightComputerConfig.Yaw.OV.Min, FlightComputerConfig.Yaw.OV.Max };
                    default: throw new NotImplementedException();
                }
            }
            throw new NotImplementedException();
        }

        private void TickTimer_Tick(object sender, EventArgs e)
        {
            if (Indicator != null)
            {
                double current_x = Width;
                double x_size = Width / NUM_FRAMES;

                int childIndex = NUM_FRAMES;
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
                        var s = (Line)Children[childIndex - NUM_FRAMES];

                        if (!double.IsNaN(GetValueForIndicator(current)) && !double.IsNaN(GetValueForIndicator(last)))
                        {
                            l.Stroke = Brushes.Blue;

                            l.X1 = current_x;
                            l.X2 = current_x - x_size;
                            l.Y2 = Math2.MapValue(GetRangeForIndicator()[0], GetRangeForIndicator()[1], Height, 0, GetValueForIndicator(current));
                            l.Y1 = Math2.MapValue(GetRangeForIndicator()[0], GetRangeForIndicator()[1], Height, 0, GetValueForIndicator(last));

                            if (Type == IndicatorChartType.Value)
                            {
                                s.X1 = l.X1;
                                s.X2 = l.X2;

                                if (!double.IsNaN(GetSetPointForIndicator(current)) && !double.IsNaN(GetSetPointForIndicator(last)))
                                {
                                    s.Y1 = Math2.MapValue(GetRangeForIndicator()[0], GetRangeForIndicator()[1], Height, 0, GetSetPointForIndicator(current));
                                    s.Y2 = Math2.MapValue(GetRangeForIndicator()[0], GetRangeForIndicator()[1], Height, 0, GetSetPointForIndicator(last));
                                }
                                else
                                {
                                    s.X1 = s.X2 = s.Y1 = s.Y2 = 0;
                                }
                            }
                        }
                        else
                        {
                            if (Type == IndicatorChartType.Value)
                            {
                                s.X1 = s.X2 = s.Y1 = s.Y2 = 0;
                            }

                            if (Type != IndicatorChartType.Output)
                            {
                                l.Stroke = Brushes.Gray;
                                l.X1 = current_x;
                                l.X2 = current_x;
                                l.Y1 = 0;
                                l.Y2 = Height;
                            }
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
