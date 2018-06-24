using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class PrimaryFlightDisplayControl : UserControl
    {
        Polygon _top = new Polygon();
        Polygon _bottom = new Polygon();
        DispatcherTimer _fpsTimer = new DispatcherTimer();
        Line _pitchLine = new Line();


        public PrimaryFlightDisplayControl()
        {
            InitializeComponent();

            _pitchLine.Stroke = Brushes.White;
            _pitchLine.StrokeThickness = 4;
            _top.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0480D8"));
            _bottom.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#744C07"));

            PFD.Children.Add(_top);
            PFD.Children.Add(_bottom);
           // PFD.Children.Add(_pitchLine);

            _fpsTimer.Interval = TimeSpan.FromMilliseconds(App.FPS);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            LayoutBackground();
        }

        private void LayoutBackground()
        {
            var pitch_mult = 2;
            var rightSkew = -1 * Timeline.PitchAvg * pitch_mult;
            var leftSkew = -1 * Timeline.PitchAvg * pitch_mult;

            var roll_mult = 4;
            rightSkew += Timeline.RollAvg * roll_mult;
            leftSkew -= Timeline.RollAvg * roll_mult;

            _top.Points.Clear();

            _top.Points.Add(new Point(0, 0));
            _top.Points.Add(new Point(PFD.ActualWidth, 0));
            _top.Points.Add(new Point(PFD.ActualWidth, PFD.ActualHeight / 2 - rightSkew));
            _top.Points.Add(new Point(0, PFD.ActualHeight / 2 - leftSkew));

            _bottom.Points.Clear();
            _bottom.Points.Add(new Point(0, PFD.ActualHeight / 2 - leftSkew));
            _bottom.Points.Add(new Point(PFD.ActualWidth, PFD.ActualHeight / 2 - rightSkew));
            _bottom.Points.Add(new Point(PFD.ActualWidth, PFD.ActualHeight));
            _bottom.Points.Add(new Point(0, PFD.ActualHeight));

            _pitchLine.X1 = PFD.ActualWidth * 0.25;
            _pitchLine.X2 = PFD.ActualWidth * 0.75;

            _pitchLine.Y1 = PFD.ActualHeight / 2 - leftSkew;
            _pitchLine.Y2 = PFD.ActualHeight / 2 - rightSkew;
        }
    }
}
