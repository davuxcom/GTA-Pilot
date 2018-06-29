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
    /// Interaction logic for RecorderWindow.xaml
    /// </summary>
    public partial class RecorderWindow : Window, ICanTick
    {
        public RecorderWindow()
        {
            InitializeComponent();

            App.Register(this);
        }

        public void Tick()
        {
            lblFramesInBuffer.Text = "" + SystemManager.Instance.Recorder.BufferedFrames;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SystemManager.Instance.App.Stop();
        }
    }
}
