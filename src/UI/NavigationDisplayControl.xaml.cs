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
    public partial class NavigationDisplayControl : UserControl, ICanTick
    {
        DispatcherTimer _locationTimer = new DispatcherTimer();
        TimelineFrame _lastRenderedFrame = null;
        Point lastPoint;

        public NavigationDisplayControl()
        {
            InitializeComponent();
            App.Register(this);

            img.Source = new BitmapImage(Metrics.Map_Zoom4_Full_20);

            MouseWheel += NavigationDisplayControl_MouseWheel;

            DrawFlightPlanLines();

            var st = imgHost.Get<ScaleTransform>();
            st.ScaleX = st.ScaleY = 4;

            _locationTimer.Interval = TimeSpan.FromSeconds(2);
            _locationTimer.Tick += LocationTimer_Tick;
            _locationTimer.Start();
        }

        private void NavigationDisplayControl_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var st = imgHost.Get<ScaleTransform>();
            double zoom = e.Delta > 0 ? .2 : -.2;
            if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                return;

            st.ScaleX += zoom;
            st.ScaleY += zoom;

            Tick();
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

        public void Tick()
        {
            var pt = Timeline.CurrentLocation;
            // Convert from map cords to 20% cords
            pt = new System.Drawing.PointF(pt.X / FlightPlanBuidler.FlightPlanScaleFactor, pt.Y / FlightPlanBuidler.FlightPlanScaleFactor);

            var st = imgHost.Get<ScaleTransform>();

            // Extend such that our center point is at the bottom of the viewport.
            pt = pt.ExtendAlongHeading(Timeline.Heading, 220 / st.ScaleX);

            var tt = imgHost.Get<TranslateTransform>();

            tt.X = -1 * (pt.X * st.ScaleX - 250) ;
            tt.Y = -1 * (pt.Y * st.ScaleY - 250) ;

           var rt = rotHost.Get<RotateTransform>();
           rt.Angle = -1 * Timeline.Heading;

            HeadingText.Text = "" + Math.Round(Timeline.Heading);
        }
    }
}
