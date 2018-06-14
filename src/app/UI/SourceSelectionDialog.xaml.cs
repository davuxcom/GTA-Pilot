using System.Windows;

namespace GTAPilot
{
    public partial class SourceSelectionDialog : Window
    {
        public SourceType Result = SourceType.Invalid;

        public SourceSelectionDialog()
        {
            InitializeComponent();
        }

        private void Live_Click(object sender, RoutedEventArgs e)
        {
            Result =  SourceType.Live;
            Close();
        }

        private void Recording_Click(object sender, RoutedEventArgs e)
        {
            Result = SourceType.Playback;
            Close();
        }

        private void Capture_Click(object sender, RoutedEventArgs e)
        {
            Result = SourceType.Capture;
            Close();
        }
    }

    public enum SourceType
    {
        Invalid,
        Live,
        Capture,
        Playback
    }
}
