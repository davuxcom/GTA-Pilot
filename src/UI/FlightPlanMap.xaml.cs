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

        public RelayCommand Clear { get; }
        public RelayCommand RW03Departure { get; }
        public RelayCommand RW30RApproach { get; }


        Point lastPoint;

        public FlightPlanMap(System.Drawing.PointF[] points, System.Drawing.PointF[] locationPoints = null)
        {
            InitializeComponent();

            img.Source = new BitmapImage(Metrics.Map_Zoom4_Full_20);

            Positions = new ObservableCollection<Position>();
            Points = new List<System.Drawing.PointF>();

            foreach (var p in points) AddPosition_FullCoordinates(p);

            if (locationPoints != null)
            {
                foreach (var p in locationPoints) RenderLocation(p);
            }

            DataContext = this;

            Clear = new RelayCommand(ClearPoints);

            RW03Departure = new RelayCommand(() =>
            {
                AddPosition_FullCoordinates(new System.Drawing.PointF { X = 2029, Y = 4575 });
                AddPosition_FullCoordinates(new System.Drawing.PointF { X = 2230, Y = 4230 });
            });

            RW30RApproach = new RelayCommand(() =>
            {
                // var RW30R_Start = new System.Drawing.PointF { X = 2290, Y = 4730 };
                //var RW30R_End = new System.Drawing.PointF { X = 2034, Y = 4583 };

                var RW30R_Start = new System.Drawing.PointF { X = 2261, Y = 4776 };
                var RW30R_End = new System.Drawing.PointF { X = 2003, Y = 4626 };
                var oppositeHdg = Math2.GetPolarHeadingFromLine(RW30R_Start, RW30R_End);

                for (var i = 8; i >= 1; i--)
                {
                    System.Drawing.PointF nextPt = RW30R_Start.ExtendAlongHeading(oppositeHdg, 50 * i);

                    AddPosition_FullCoordinates(nextPt);
                }
                AddPosition_FullCoordinates(RW30R_Start);
                AddPosition_FullCoordinates(RW30R_End);
            });
        }

        System.Drawing.PointF lastLocation;
        private void RenderLocation(System.Drawing.PointF frame)
        {
            if (lastLocation != default(System.Drawing.PointF))
            {
                Line l = new Line();
                l.Stroke = Brushes.Red;
                l.StrokeThickness = 1;

                l.X1 = lastLocation.X / Metrics.SCALE_Map4_20_TO_100;
                l.X2 = frame.X / Metrics.SCALE_Map4_20_TO_100;
                l.Y1 = lastLocation.Y / Metrics.SCALE_Map4_20_TO_100;
                l.Y2 = frame.Y / Metrics.SCALE_Map4_20_TO_100;

                canvas.Children.Add(l);
            }
            lastLocation = frame;
        }

        private void ClearPoints()
        {
            Positions.Clear();
            Points.Clear();
            canvas.Children.Clear();
            lastPoint = default(Point);
        }

        private void img_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AddPosition(e.GetPosition(img));
        }

        private void AddPosition_FullCoordinates(System.Drawing.PointF pt)
        {
            AddPosition(new Point(pt.X / Metrics.SCALE_Map4_20_TO_100, 
                pt.Y / Metrics.SCALE_Map4_20_TO_100));
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
