using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class MainWindow : Window
    {
        class LocalTraceListener : TraceListener
        {
            Action<string> _handler;

            public LocalTraceListener(Action<string> handler) => _handler = handler;

            public override void Write(string message) => WriteLine(message);
            public override void WriteLine(string message) => _handler(message);
        }

        MainWindowViewModel _viewModel;
        DispatcherTimer _fpsTimer = new DispatcherTimer();
        SaveFrameConsumer _captureSink;

        internal MainWindow()
        {
            InitializeComponent();

            Activated += MainWindow_Activated;

            Trace.Listeners.Clear();
            Trace.Listeners.Add(new LocalTraceListener(OnMessage));
        }

        private void OnMessage(string msg)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                bool isAutoScroll = lstLog.Items.Count == 0 || lstLog.Items.Count - 1 == lstLog.SelectedIndex;

                lstLog.Items.Add(msg);

                if (isAutoScroll)
                {
                    lstLog.SelectedIndex = lstLog.Items.Count - 1;

                    if (VisualTreeHelper.GetChildrenCount(lstLog) > 0)
                    {
                        Border border = (Border)VisualTreeHelper.GetChild(lstLog, 0);
                        ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                        scrollViewer.ScrollToBottom();
                    }
                }
            }));
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            Activated -= MainWindow_Activated;

            Topmost = true;
            Topmost = false;

            var dlg = new SourceSelectionDialog();
            dlg.ShowDialog();

            FridaController fridaController = null;
            SystemManager mgr;
            if (dlg.Result == SourceType.Live)
            {
                // TODO: pipe through screen textbox
                fridaController = new FridaController();
                mgr = new SystemManager(new DesktopFrameProducer(), fridaController);
            }
            else if (dlg.Result == SourceType.Capture)
            {
                // TODO: pipe through screen textbox
                _captureSink = new SaveFrameConsumer(dlg.txtCaptureLocation.Text);
                fridaController = new FridaController();
                mgr = new SystemManager(new DesktopFrameProducer(), _captureSink.HandleFrameArrived, fridaController);
            }
            else if (dlg.Result == SourceType.Playback)
            {
                mgr = new SystemManager(new ReplayFrameProducer(dlg.txtRecording.Text, dlg.txtFrameSet.Text), null);
            }
            else throw new NotImplementedException();

            _viewModel = new MainWindowViewModel(mgr, _captureSink, fridaController);
            DataContext = _viewModel;

            _fpsTimer.Interval = TimeSpan.FromMilliseconds(App.FPS);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();

            if (dlg.Result == SourceType.Capture)
            {
                var progress = new CaptureProgress();

                progress.ShowDialog();

                mgr.StopCapture();
            }
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            _viewModel.Tick();
        }

        private void SaveFrameSet_Click(object sender, RoutedEventArgs e)
        {
            var indicator = (IndicatorViewModel)((FrameworkElement)sender).DataContext;

            var frames = string.Join("\r\n", indicator.BadFrames.Select(f => f.ToString()));

            var fn = Path.GetTempFileName() + ".txt";
            File.WriteAllText(fn, frames);

            Process.Start(fn);
        }

        private void Border_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var viewModel = (IndicatorViewModel)((FrameworkElement)sender).DataContext;

            var window = new IndicatorWindow(viewModel);
            window.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var m = new MapWindow();
            m.ShowDialog();
        }
    }
}
