using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GTAPilot
{
    /// <summary>
    /// Interaction logic for SourceSelectionDialog.xaml
    /// </summary>
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
            Result = @"c:\save\recording1";
            Close();
        }
    }
}
