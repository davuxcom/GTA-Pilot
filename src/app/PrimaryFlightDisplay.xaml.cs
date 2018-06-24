using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GTAPilot
{
    public partial class PrimaryFlightDisplay : Window
    {

        public PrimaryFlightDisplay()
        {
            InitializeComponent();

            img.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full_20.png")));
            border.ScaleTo(8);
            border.PanTo(Timeline.StartLocation, 30);


        }
        
    }
}
