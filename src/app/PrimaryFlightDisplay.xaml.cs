using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GTAPilot
{
    public partial class PrimaryFlightDisplay : Window
    {
        Polygon _top = new Polygon();
        Polygon _bottom = new Polygon();
        Ellipse _circle = new Ellipse();

        public PrimaryFlightDisplay()
        {
            InitializeComponent();

            _top.Fill = Brushes.SkyBlue;
            _bottom.Fill = Brushes.Brown;
            _circle.Stroke = Brushes.White;
            _circle.StrokeThickness = 4;
            _circle.Height = 500;
            _circle.Width = 500;
            Canvas.SetLeft(_circle, PFD.Width / 2 - _circle.Width / 2);
            Canvas.SetTop(_circle, PFD.Height / 2 - _circle.Height / 2);

            LayoutBackground();

            PFD.Children.Add(_top);
            PFD.Children.Add(_bottom);
            PFD.Children.Add(_circle);
        }

        private void LayoutBackground()
        {
            var pitch_mult = 4;
            var rightSkew = -1 * Pitch.Value * pitch_mult;
            var leftSkew = -1 * Pitch.Value * pitch_mult;

            var roll_mult = 4;
            rightSkew -= Roll.Value * roll_mult;
            leftSkew += Roll.Value * roll_mult;

            _top.Points.Clear();

            _top.Points.Add(new Point(0, 0));
            _top.Points.Add(new Point(PFD.Width, 0));
            _top.Points.Add(new Point(PFD.Width, PFD.Height / 2 - rightSkew));
            _top.Points.Add(new Point(0, PFD.Height / 2 - leftSkew));

            _bottom.Points.Clear();
            _bottom.Points.Add(new Point(0, PFD.Height / 2 - leftSkew));
            _bottom.Points.Add(new Point(PFD.Width, PFD.Height / 2 - rightSkew));
            _bottom.Points.Add(new Point(PFD.Width, PFD.Height));
            _bottom.Points.Add(new Point(0, PFD.Height));

        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = (Slider)sender;

            LayoutBackground();
        }
    }
}
