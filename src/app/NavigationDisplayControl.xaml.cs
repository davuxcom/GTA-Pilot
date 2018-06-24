using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class NavigationDisplayControl : UserControl
    {
        DispatcherTimer _fpsTimer = new DispatcherTimer();

        public NavigationDisplayControl()
        {
            InitializeComponent();

            img.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full_20.png")));
            border.ScaleTo(10);

            _fpsTimer.Interval = TimeSpan.FromMilliseconds(App.FPS);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            border.PanTo(Timeline.CurrentLocation, Timeline.Heading);
        }
    }
}
