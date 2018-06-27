using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GTAPilot
{
    public class IndicatorChart : Canvas, ICanTick
    {
        public enum IndicatorChartType
        {
            Value, Delay, InputOutput
        }

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

        int NUM_FRAMES = 250 / 4 + 1; // even with Width, hack
        private Line zeroLine;
        private Line topLine;
        private Line bottomLine;
        private TextBlock topText = new TextBlock { FontSize = 12 };
        private TextBlock bottomText = new TextBlock { FontSize = 12};

        int SKIP_LINES = 5;

        public IndicatorChart()
        {
            App.Register(this);

            {
                var l = new Line();
                l.StrokeThickness = 1;
                l.Stroke = Brushes.DarkGray;
                Children.Add(l);
                zeroLine = l;
            }

            {
                var l = new Line();
                l.StrokeThickness = 1;
                l.Stroke = Brushes.DarkGray;
                Children.Add(l);
                topLine = l;
            }

            {
                var l = new Line();
                l.StrokeThickness = 1;
                l.Stroke = Brushes.DarkGray;
                Children.Add(l);
                bottomLine = l;
            }

            Children.Add(topText);
            Children.Add(bottomText);

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
            else if (Type == IndicatorChartType.InputOutput)
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
            else if (Type == IndicatorChartType.InputOutput)
            {
                return GetTimelineValueForIndicator(frame).InputValue;
            }


            return double.NaN;
        }

        private double[] GetRangeForIndicator()
        {
            if (Type == IndicatorChartType.Delay)
            {
                return new double[] { 0, 0.1 }; // seconds
            }
            else if (Type == IndicatorChartType.Value)
            {
                switch (Indicator.Type)
                {
                    case IndicatorType.Roll: return new double[] { -45, 45 };
                    case IndicatorType.Pitch: return new double[] { -90, 90 };
                    case IndicatorType.Speed: return new double[] { 0, 180 };
                    case IndicatorType.Altitude: return new double[] { 0, 8500 };
                    case IndicatorType.Yaw: return new double[] { 0, 360 };
                    default: throw new NotImplementedException();
                }
            }
            else if (Type == IndicatorChartType.InputOutput)
            {
                switch (Indicator.Type)
                {
                    case IndicatorType.Roll: return new double[] { FlightComputerConfig.Roll.OV.Min, FlightComputerConfig.Roll.OV.Max };
                    case IndicatorType.Pitch: return new double[] { FlightComputerConfig.Pitch.OV.Min, FlightComputerConfig.Pitch.OV.Max };
                    case IndicatorType.Speed: return new double[] { FlightComputerConfig.Speed.OV.Min, FlightComputerConfig.Speed.OV.Max };
                    case IndicatorType.Altitude: return new double[] { -1, 1 };
                    case IndicatorType.Yaw: return new double[] { -20, 20 };
                    default: throw new NotImplementedException();
                }
            }
            throw new NotImplementedException();
        }

        public void Tick()
        {
            if (Indicator != null)
            {
                double current_x = Width - 1;
                double x_size = 250 / NUM_FRAMES; // Width / NUM_FRAMES;

                zeroLine.X1 = 0;
                zeroLine.X2 = Width;
                zeroLine.Y1 = Height / 2;
                zeroLine.Y2 = Height / 2;

                topLine.X1 = 0;
                topLine.X2 = Width;
                topLine.Y1 = 0;
                topLine.Y2 = 0;

                bottomLine.X1 = 0;
                bottomLine.X2 = Width;
                bottomLine.Y1 = Height;
                bottomLine.Y2 = Height;

                bottomText.Text = "" + GetRangeForIndicator()[0];
                topText.Text = "" + GetRangeForIndicator()[1];

                Canvas.SetTop(topText, 4);
                Canvas.SetLeft(topText, 4);

                Canvas.SetTop(bottomText, Height - 20);
                Canvas.SetLeft(bottomText, 4);

                int childIndex = SKIP_LINES + NUM_FRAMES;
                TimelineFrame last = null;
                for (var i = Timeline.LatestFrameId; i >= 0 && i > Timeline.LatestFrameId - NUM_FRAMES; i--)
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

                            if (Type == IndicatorChartType.Delay)
                            {
                                if (GetValueForIndicator(current) > 0.2)
                                {
                                    l.Stroke = Brushes.Red;
                                }
                                else
                                {
                                    l.Stroke = Brushes.Green;
                                }
                            }


                            l.X1 = current_x;
                            l.X2 = current_x - x_size;
                            l.Y2 = Math2.MapValue(GetRangeForIndicator()[0], GetRangeForIndicator()[1], Height, 0, GetValueForIndicator(current));
                            l.Y1 = Math2.MapValue(GetRangeForIndicator()[0], GetRangeForIndicator()[1], Height, 0, GetValueForIndicator(last));


                        }
                        else
                        {
                            if (Type == IndicatorChartType.Value)
                            {
                                s.X1 = s.X2 = s.Y1 = s.Y2 = 0;
                            }

                            if (Type != IndicatorChartType.InputOutput)
                            {
                                l.Stroke = Brushes.Gray;
                                l.X1 = current_x;
                                l.X2 = current_x;
                                l.Y1 = 0;
                                l.Y2 = Height;
                            }
                            else
                            {
                                l.X1 = l.X2 = l.Y1 = l.Y2 = 0;
                            }
                        }

                        if (Type == IndicatorChartType.Value || Type == IndicatorChartType.InputOutput)
                        {
                            s.X1 = current_x;
                            s.X2 = current_x - x_size;

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

                        current_x = current_x - x_size;
                        childIndex++;
                        last = current;
                    }

                }
            }
        }
    }
}
