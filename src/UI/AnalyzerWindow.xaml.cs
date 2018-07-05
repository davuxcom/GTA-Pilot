using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GTAPilot
{
    public partial class AnalyzerWindow : Window, ICanTick
    {
        static AnalyzerWindow _instance;
        AnalyzerViewModel _viewModel;

        internal AnalyzerWindow()
        {
            _instance = this;

            InitializeComponent();

            Activated += MainWindow_Activated;

            Closing += Window_Closing;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            Activated -= MainWindow_Activated;

            Topmost = true;
            Topmost = false;

            _viewModel = new AnalyzerViewModel();
            DataContext = _viewModel;

            App.Register(this);
        }

        int ticks = 0;
        public void Tick()
        {
            ticks++;
            if (ticks % 4 == 0) _viewModel.Tick();
        }

        private void Border_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var viewModel = (IndicatorViewModel)((FrameworkElement)sender).DataContext;

            var window = new IndicatorWindow(viewModel);
            window.Owner = this;
            window.ShowDialog();
        }

        private void TextBox_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var txt = (TextBox)sender;

            double next = 0;

            var vl = double.Parse(txt.Text);

            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                next = (vl + Math.Sign(e.Delta) * 0.01);
            }
            else
            {
                next = (vl + Math.Sign(e.Delta) * 0.1);
            }

            txt.Text = next.ToString();
        }

        private void ReplaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = (Slider)sender;
            SystemManager.Instance.Replay.Seek((int)slider.Value);
        }

        internal static void Raise()
        {
            _instance.Dispatcher.Invoke(() =>
            {
                _instance.Show();
            });
        }
    }
}
