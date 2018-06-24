using GTAPilot.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class NavigationDisplayControl : UserControl
    {
        DispatcherTimer _fpsTimer = new DispatcherTimer();
        Point lastPoint;

        public NavigationDisplayControl()
        {
            InitializeComponent();

            img.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full_20.png")));
            border.ScaleTo(10);

            DrawFlightPlanLines();

            _fpsTimer.Interval = TimeSpan.FromMilliseconds(App.FPS);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();
        }

        private void DrawFlightPlanLines()
        {
            foreach(var p in SystemManager.Instance.FlightPlan.Points)
            {
                AddPosition(new Point(p.X / FlightPlanBuidler.FlightPlanScaleFactor, p.Y / FlightPlanBuidler.FlightPlanScaleFactor));
            }
        }


        private void AddPosition(Point pt)
        {
            double heading = double.NaN;
            if (lastPoint != default(Point))
            {
                var l = new Line
                {
                    X1 = pt.X,
                    Y1 = pt.Y,
                    X2 = lastPoint.X,
                    Y2 = lastPoint.Y
                };
                l.Stroke = Brushes.Blue;
                l.StrokeThickness = 1;
                canvas.Children.Insert(canvas.Children.Count - 1, l);

                heading = Math2.GetPolarHeadingFromLine(pt.ToPointF(), lastPoint.ToPointF());
            }

            Ellipse dot = new Ellipse();
            dot.Fill = Brushes.Red;
            dot.Height = dot.Width = 2;
            Canvas.SetTop(dot, pt.Y - dot.Height / 2);
            Canvas.SetLeft(dot, pt.X - dot.Width / 2);
            canvas.Children.Add(dot);
            lastPoint = pt;
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            border.PanTo(Timeline.CurrentLocation, Timeline.Heading);
        }
    }
}
