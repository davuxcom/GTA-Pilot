using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GTAPilot
{
    public partial class PrimaryFlightDisplayControl : UserControl, ICanTick
    {
        Polygon _top = new Polygon();
        Polygon _bottom = new Polygon();
        List<Line> _pitchLines = new List<Line>();

        public PrimaryFlightDisplayControl()
        {
            InitializeComponent();
            App.Register(this);

            _top.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0480D8"));
            _bottom.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#744C07"));

            PFDBackground.Children.Add(_top);
            PFDBackground.Children.Add(_bottom);

            for (var i = 0; i < 40; i++)
            {
                var line = new Line { Stroke = Brushes.White, StrokeThickness = 2 };

                _pitchLines.Add(line);
                PFDBackground.Children.Add(line);
            }

            TransformGroup group = new TransformGroup();
            group.Children.Add(new TranslateTransform());
            group.Children.Add(new RotateTransform());

            PFDBackground.RenderTransform = group;
            PFDBackground.RenderTransformOrigin = new Point(0.5, 0.5);

            SizeChanged += PrimaryFlightDisplayControl_SizeChanged;

            for (var i = 18; i >= 0; i--)
            {
                SpeedTapeRoot.Children.Add(CreateTemplateFor(i * 10));
            }

            currentY = 0;
            for (var i = 85; i >= 0; i--)
            {
                AltitudeTapeRoot.Children.Add(CreateTemplateFor(i * 100));
            }
        }

        private void PrimaryFlightDisplayControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _top.Points.Clear();

            _top.Points.Add(new Point(-200, -200));
            _top.Points.Add(new Point(PFDBackground.ActualWidth + 200, -200));
            _top.Points.Add(new Point(PFDBackground.ActualWidth + 200, PFDBackground.ActualHeight / 2));
            _top.Points.Add(new Point(-200, PFDBackground.ActualHeight / 2));

            _bottom.Points.Clear();
            _bottom.Points.Add(new Point(-200, PFDBackground.ActualHeight / 2));
            _bottom.Points.Add(new Point(PFDBackground.ActualWidth + 200, PFDBackground.ActualHeight / 2));
            _bottom.Points.Add(new Point(PFDBackground.ActualWidth + 200, PFDBackground.ActualHeight + 200));
            _bottom.Points.Add(new Point(-200, PFDBackground.ActualHeight + 200));

            var start = 0;
            var step = 500 / 6;
            for (var i = 0; i < 25; i += 4)
            {
                var l = _pitchLines[i];
                l.X1 = 300;
                l.X2 = 400;
                l.Y1 = start;
                l.Y2 = start;
                var l2 = _pitchLines[i + 1];
                l2.X1 = 330;
                l2.X2 = 370;
                l2.Y1 = start + (step / 4) * 1;
                l2.Y2 = start + (step / 4) * 1;
                var l3 = _pitchLines[i + 2];
                l3.X1 = 320;
                l3.X2 = 380;
                l3.Y1 = start + (step / 4) * 2;
                l3.Y2 = start + (step / 4) * 2;
                var l4 = _pitchLines[i + 3];
                l4.X1 = 330;
                l4.X2 = 370;
                l4.Y1 = start + (step / 4) * 3;
                l4.Y2 = start + (step / 4) * 3;
                start += step;
            }
        }

        private void LayoutBackground()
        {
            var rt = PFDBackground.GetTransform<RotateTransform>();
            var tt = PFDBackground.GetTransform<TranslateTransform>();

            var pitch_mult = 1.8;
            var roll_mult = -1;

            tt.Y = Timeline.PitchAvg * pitch_mult;

            var roll = Timeline.RollAvg;
            if (roll < 0)
            {
                rt.Angle = (360 + roll) * roll_mult;
            }
            else
            {
                rt.Angle = roll * roll_mult;
            }
        }

        public void Tick()
        {
            LayoutBackground();

            AltitudeText.Text = "" + Math.Round(Timeline.Altitude);
            SpeedText.Text = "" + Math.Round(Timeline.Speed);

            var mcp = SystemManager.Instance.MCP;

            TModeText.Text = mcp.IASHold ? "SPD" : "";
            VModeText.Text = mcp.VSHold ? "VS" :
                (mcp.AltitudeHold ? "ALT" : "");
            LModeText.Text = mcp.HeadingHold ? "HDG SEL" :
                (mcp.LNAV ? "LNAV" : "");

            if (SystemManager.Instance.Nav.IsOnGlidePath)
            {
                VModeText.Text = "G/P";
                LModeText.Text = "FAC";
            }

            AltSetpoint.Text = "" + Math.Round(mcp.ALT);
            SpeedSetpoint.Text = "" + Math.Round(mcp.IAS);

            if (!double.IsNaN(Timeline.Pitch) &&
                !double.IsNaN(Timeline.Roll) &&
                 !double.IsNaN(mcp.VS))
            {
                var top = -1 * (mcp.VS - Timeline.PitchAvg) * 4;
                FDH.Margin = new Thickness(0, top, 0, 0);
                var left = (mcp.Bank - Timeline.RollAvg) * 4;
                FDV.Margin = new Thickness(left, 0, 0, 0);
            }

            GPDisplay.Visibility = (SystemManager.Instance.Nav.IsOnGlidePath) ? Visibility.Visible : Visibility.Collapsed;
            FACDisplay.Visibility = (SystemManager.Instance.Nav.IsOnGlidePath) ? Visibility.Visible : Visibility.Collapsed;
            if (SystemManager.Instance.Nav.IsOnGlidePath)
            {
                var vx = (mcp.ALT - Timeline.AltitudeAvg) * -2;
                ILSV.Margin = new Thickness(0, vx, 0, 0);

                var hx = SystemManager.Instance.Nav.DistanceFromTargetLine * -25;
                ILSH.Margin = new Thickness(hx, 0, 0, 0);
            }

            if (!double.IsNaN(Timeline.Pitch))
            {
                var pitch = Timeline.PitchAvg * 0.8;

                VSLine.Y1 = 250 - pitch;

                VSLine.Stroke = Math.Abs(Timeline.Pitch) > 60 ? Brushes.Yellow : Brushes.White;
            }

            {
                var tt = (TranslateTransform)SpeedTapeRoot.RenderTransform;
                tt.Y = -1 * TAPE_ITEM_HEIGHT * ((180 - Timeline.SpeedAvg) / 10) + 165;
            }
            {
                var tt = (TranslateTransform)AltitudeTapeRoot.RenderTransform;
                tt.Y = -1 * TAPE_ITEM_HEIGHT * ((8500 - Timeline.AltitudeAvg) / 100) + 165;
            }
        }

        double TAPE_ITEM_HEIGHT = 60;

        double currentY = 0;
        private Grid CreateTemplateFor(int number)
        {
            var g = new Grid();
            var t = new TextBlock();
            t.Text = "" + number;
            t.Margin = new Thickness(12, 0, 0, 0);
            t.HorizontalAlignment = HorizontalAlignment.Left;
            t.VerticalAlignment = VerticalAlignment.Center;
            t.Foreground = Brushes.White;
            t.FontSize = 20;

            g.Children.Add(t);

            var top = new Rectangle();
            top.Stroke = Brushes.White;
            top.StrokeThickness = 1;
            top.HorizontalAlignment = HorizontalAlignment.Right;
            top.VerticalAlignment = VerticalAlignment.Top;
            top.Width = 10;
            var bottom = new Rectangle();
            bottom.Stroke = Brushes.White;
            bottom.StrokeThickness = 1;
            bottom.HorizontalAlignment = HorizontalAlignment.Right;
            bottom.VerticalAlignment = VerticalAlignment.Bottom;
            bottom.Width = 10;
            var center = new Rectangle();
            center.Stroke = Brushes.White;
            center.StrokeThickness = 1;
            center.HorizontalAlignment = HorizontalAlignment.Right;
            center.VerticalAlignment = VerticalAlignment.Center;
            center.Width = 20;

            g.Children.Add(top);
            g.Children.Add(center);
            g.Children.Add(bottom);
            g.Height = TAPE_ITEM_HEIGHT;
            g.Width = 75;
            Canvas.SetTop(g, currentY);
            currentY += TAPE_ITEM_HEIGHT;
            return g;
        }
    }
}
