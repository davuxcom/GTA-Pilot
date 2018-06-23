using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GTAPilot
{
    public partial class FlightPlanBuidler : Window
    {
        public class Position
        {
            public Point pt;
            public double Heading;

            public override string ToString()
            {
                return $"{(int)pt.X}, {(int)pt.Y} Heading={Math.Round(Heading, 2)}";
            }
        }

        public ObservableCollection<Position> Positions { get; }

        public FlightPlanBuidler()
        {
            InitializeComponent();

            img.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full_20.png")));

            Positions = new ObservableCollection<Position>();

            var start = new Point(2030.2f, 4573.9f);

            AddPosition(new Point(start.X / 5, start.Y / 5));
            AddPosition(new Point(426, 880));

            DataContext = this;
        }

        Point lastPoint;

        private void img_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AddPosition(e.GetPosition(img));
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
                canvas.Children.Add(l);

                heading = Math.Atan2(l.Y2 - l.Y1, l.X2 - l.X1);
                // Starting at 9PM clockwise to 3PM: 0 to pi
                // Start at 9PM counter-clockwise to 3PM: 0 to -pi

                if (heading >= 0)
                {
                    // 0 to 180
                    heading = (heading * (180 / Math.PI));
                }
                else
                {
                    // 0 to -180
                    heading = heading * (180 / Math.PI);
                    // 180 to 0
                    heading = 180 + heading;
                    // 360 to 180
                    heading = heading + 180;
                }

                // Skew so 0 is at 12PM.
                heading -= 90;
                if (heading < 0) heading = 360 + heading;
            }

            Ellipse dot = new Ellipse();
            dot.Fill = Brushes.Red;
            dot.Height = dot.Width = 2;
            Canvas.SetTop(dot, pt.Y - dot.Height / 2);
            Canvas.SetLeft(dot, pt.X - dot.Width / 2);
            canvas.Children.Add(dot);
            lastPoint = pt;

            Positions.Add(new Position { pt = pt, Heading = heading });
        }
    }
}
