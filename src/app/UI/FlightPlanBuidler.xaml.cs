using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GTAPilot.Extensions;

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

            Positions.Add(new Position { pt = pt, Heading = heading });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder ret = new StringBuilder();
            foreach(var p in Positions)
            {
                ret.AppendLine($"{p.pt.X},{p.pt.Y}");
            }

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "FlightPlan";
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt"; 

            if (dlg.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dlg.FileName, ret.ToString());
            }
        }
    }
}
