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
        Ellipse _circle = new Ellipse();
        DispatcherTimer _fpsTimer = new DispatcherTimer();


        public PrimaryFlightDisplayControl()
        {
            InitializeComponent();

            _top.Fill = Brushes.SkyBlue;
            _bottom.Fill = Brushes.Brown;
            _circle.Stroke = Brushes.White;
            _circle.StrokeThickness = 4;
            _circle.Height = Width / 2;
            _circle.Width = Width / 2;
            Canvas.SetLeft(_circle, PFD.Width / 2 - _circle.Width / 2);
            Canvas.SetTop(_circle, PFD.Height / 2 - _circle.Height / 2);

            LayoutBackground();

            PFD.Children.Add(_top);
            PFD.Children.Add(_bottom);
            PFD.Children.Add(_circle);

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

        }
    }
}
