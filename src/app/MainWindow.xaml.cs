using System;
using System.Windows;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class MainWindow : Window
    {
        MainWindowViewModel _viewModel;
        DispatcherTimer _fpsTimer = new DispatcherTimer();

        internal MainWindow()
        {
            InitializeComponent();

            Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            Activated -= MainWindow_Activated;

            var dlg = new SourceSelectionDialog();
            dlg.ShowDialog();

            SystemManager mgr;
            if (dlg.Result == null)
            {
                // TODO: pipe through screen textbox
                mgr = new SystemManager(new DesktopFrameProducer());
            }
            else
            {
                // TODO: pipe through frameset
                mgr = new SystemManager(new ReplayFrameProducer(dlg.Result));
            }

            _viewModel = new MainWindowViewModel(new SystemManager(new ReplayFrameProducer(@"c:\save\recording1")));
            DataContext = _viewModel;

            _fpsTimer.Interval = TimeSpan.FromMilliseconds(1000 / 40);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();

            // do something
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
