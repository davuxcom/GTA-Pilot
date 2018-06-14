using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class MapWindow : Window
    {
        DispatcherTimer _timer = new DispatcherTimer();
        TimelineFrame _lastRenderedFrame = null;

        public MapWindow()
        {
            InitializeComponent();

            img.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full.png")));

            _timer.Interval = TimeSpan.FromMilliseconds(1000 / 60);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
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

        private void RenderFrame(TimelineFrame frame)
        {
            Line l = new Line();
            l.Stroke = Brushes.Red;
            l.StrokeThickness = 10;

            if (_lastRenderedFrame != null)
            {
                l.X1 = _lastRenderedFrame.Location.X;
                l.X2 = frame.Location.X;
                l.Y1 = _lastRenderedFrame.Location.Y;
                l.Y2 = frame.Location.Y;
            }
            else
            {

                l.X1 = l.X2 = frame.Location.X;
                l.Y1 = l.Y2 = frame.Location.Y;
            }

            canvas.Children.Add(l);
        }
    }
}
