using System.Windows;

namespace GTAPilot
{
    public partial class SourceSelectionDialog : Window
    {
        public string Result = null;

        public SourceSelectionDialog()
        {
            InitializeComponent();
        }

        private void Live_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            Close();
        }

        private void Recording_Click(object sender, RoutedEventArgs e)
        {
            Result = txtRecording.Text;
            Close();
        }
    }
}
