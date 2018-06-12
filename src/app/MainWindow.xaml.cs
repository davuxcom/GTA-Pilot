using System;
using System.Windows;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class MainWindow : Window
    {
        MainWindowViewModel _viewModel;
        DispatcherTimer _fpsTimer = new DispatcherTimer();

        internal MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;

            _fpsTimer.Interval = TimeSpan.FromMilliseconds(200);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            _viewModel.Tick();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Save();
        }
    }
}
