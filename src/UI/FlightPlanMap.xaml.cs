using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GTAPilot
{
    public partial class FlightPlanMap : Window
    {
        public class Position
        {
            public Point pt;

            public override string ToString()
            {
                return $"{(int)pt.X}, {(int)pt.Y}";
            }
        }

        public ObservableCollection<Position> Positions { get; }
        public List<System.Drawing.PointF> Points { get; }

        public FlightPlanMap(System.Drawing.PointF[] points)
        {
            InitializeComponent();

            img.Source = new BitmapImage(Metrics.Map_Zoom4_Full_20);

            Positions = new ObservableCollection<Position>();
            Points = new List<System.Drawing.PointF>();

            foreach (var p in points) AddPosition(new Point(p.X / Metrics.SCALE_Map4_20_TO_100, p.Y / Metrics.SCALE_Map4_20_TO_100));

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

            Positions.Add(new Position { pt = pt });
            Points.Add(new System.Drawing.PointF((float)pt.X * Metrics.SCALE_Map4_20_TO_100, (float)pt.Y * Metrics.SCALE_Map4_20_TO_100));
        }
    }
}
