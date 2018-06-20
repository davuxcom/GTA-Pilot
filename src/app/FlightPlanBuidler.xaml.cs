using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GTAPilot
{
    public partial class FlightPlanBuidler : Window
    {
        public FlightPlanBuidler()
        {
            InitializeComponent();

            img.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full_20.png")));

        }

        Point lastPoint;

        private void img_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var pt = e.GetPosition(img);
            Trace.WriteLine(e.GetPosition(img));

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
                l.StrokeThickness = 2;
                canvas.Children.Add(l);
            }

            lastPoint = pt;
        }
    }
}
