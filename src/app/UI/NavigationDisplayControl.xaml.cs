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
        DispatcherTimer _locationTimer = new DispatcherTimer();
        TimelineFrame _lastRenderedFrame = null;
        Point lastPoint;

        public NavigationDisplayControl()
        {
            InitializeComponent();

            img.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full_20.png")));

            var st = imgHost.Get<ScaleTransform>();
            st.ScaleX = st.ScaleY = 14;

            DrawFlightPlanLines();

            _fpsTimer.Interval = TimeSpan.FromMilliseconds(App.FPS);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();

            _locationTimer.Interval = TimeSpan.FromSeconds(2);
            _locationTimer.Tick += LocationTimer_Tick;
            _locationTimer.Start();
        }

        private void LocationTimer_Tick(object sender, EventArgs e)
        {

            var startId = _lastRenderedFrame != null ? _lastRenderedFrame.Id : 0;
            for (var i = startId; i < Timeline.LastFrameId; i++)
            {
                if (Timeline.Data[i] != null && Timeline.Data[i].LocationComplete)
                {
                    RenderFrame(Timeline.Data[i]);

                    _lastRenderedFrame = Timeline.Data[i];
                }
                else
                {
                    break;
                }
            }
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
            var pt = Timeline.CurrentLocation;
            imgHost.RenderTransformOrigin = new Point(
            pt.X / (double)5500,
            pt.Y / (double)6000);

            var rt = imgHost.Get<RotateTransform>();
            rt.Angle = -1 * Timeline.Heading;

        }

        private void RenderFrame(TimelineFrame frame)
        {
            Line l = new Line();
            l.Stroke = Brushes.Red;
            l.StrokeThickness = 2;

            if (_lastRenderedFrame != null)
            {
                l.X1 = _lastRenderedFrame.Location.X / FlightPlanBuidler.FlightPlanScaleFactor;
                l.X2 = frame.Location.X / FlightPlanBuidler.FlightPlanScaleFactor;
                l.Y1 = _lastRenderedFrame.Location.Y / FlightPlanBuidler.FlightPlanScaleFactor;
                l.Y2 = frame.Location.Y / FlightPlanBuidler.FlightPlanScaleFactor;
            }
            else
            {

                l.X1 = l.X2 = frame.Location.X / FlightPlanBuidler.FlightPlanScaleFactor;
                l.Y1 = l.Y2 = frame.Location.Y / FlightPlanBuidler.FlightPlanScaleFactor;
            }

            canvas.Children.Add(l);
        }
    }
}
