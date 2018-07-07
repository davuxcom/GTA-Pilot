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

            var st = imgHost.GetTransform<ScaleTransform>();
            st.ScaleX = st.ScaleY = 3;

            _locationTimer.Interval = TimeSpan.FromSeconds(0.5);
            _locationTimer.Tick += LocationTimer_Tick;
            _locationTimer.Start();
        }

        private void NavigationDisplayControl_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var st = imgHost.GetTransform<ScaleTransform>();
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
            for (var i = startId; i < Timeline.LatestFrameId; i++)
            {
                if (Timeline.Data[i] != null && Timeline.Data[i].IsLocationCalculated)
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
                AddPosition(new Point(p.X / Metrics.SCALE_Map4_20_TO_100, p.Y / Metrics.SCALE_Map4_20_TO_100));
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
                l.Stroke = Brushes.Magenta;
                l.StrokeThickness = 0.4;
                canvas.Children.Insert(canvas.Children.Count - 1, l);

                heading = Math2.GetPolarHeadingFromLine(pt.ToPointF(), lastPoint.ToPointF());
            }

            Ellipse dot = new Ellipse();
            dot.Fill = Brushes.Gray;
            dot.Height = dot.Width = 1;
            Canvas.SetTop(dot, pt.Y - dot.Height / 2);
            Canvas.SetLeft(dot, pt.X - dot.Width / 2);
            canvas.Children.Add(dot);
            lastPoint = pt;
        }

        private void RenderFrame(TimelineFrame frame)
        {
            Line l = new Line();
            l.Stroke = Brushes.Red;
            l.StrokeThickness = 0.4;

            if (_lastRenderedFrame != null)
            {
                l.X1 = _lastRenderedFrame.Location.X / Metrics.SCALE_Map4_20_TO_100;
                l.X2 = frame.Location.X / Metrics.SCALE_Map4_20_TO_100;
                l.Y1 = _lastRenderedFrame.Location.Y / Metrics.SCALE_Map4_20_TO_100;
                l.Y2 = frame.Location.Y / Metrics.SCALE_Map4_20_TO_100;

                var len = Math2.GetDistance(_lastRenderedFrame.Location, frame.Location);
                if (len > 5)
                {
                    l.Stroke = Brushes.Red;
                }
            }
            else
            {

                l.X1 = l.X2 = frame.Location.X / Metrics.SCALE_Map4_20_TO_100;
                l.Y1 = l.Y2 = frame.Location.Y / Metrics.SCALE_Map4_20_TO_100;
            }




            canvas.Children.Add(l);
        }

        public void Tick()
        {
            var pt = Timeline.CurrentLocation;
            // Convert from map cords to 20% cords
            pt = new System.Drawing.PointF(pt.X / Metrics.SCALE_Map4_20_TO_100, pt.Y / Metrics.SCALE_Map4_20_TO_100);

            var st = imgHost.GetTransform<ScaleTransform>();

            // Extend such that our center point is at the bottom of the viewport.
        //    pt = pt.ExtendAlongHeading(Timeline.Heading, 220 / st.ScaleX);

            var tt = imgHost.GetTransform<TranslateTransform>();

            tt.X = -1 * (pt.X * st.ScaleX - 250) ;
            tt.Y = -1 * (pt.Y * st.ScaleY - 250) ;

           var rt = rotHost.GetTransform<RotateTransform>();
           rt.Angle = -1 * Timeline.Heading;

            HeadingText.Text = "" + Math.Round(Timeline.Heading,1);
        }
    }
}
