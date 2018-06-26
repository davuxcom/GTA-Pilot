using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class AnalyzerWindow : Window, ICanTick
    {
        class LocalTraceListener : TraceListener
        {
            Action<string> _handler;

            public LocalTraceListener(Action<string> handler) => _handler = handler;

            public override void Write(string message) => WriteLine(message);
            public override void WriteLine(string message) => _handler(message);
        }

        MainWindowViewModel _viewModel;

        internal AnalyzerWindow()
        {
            InitializeComponent();

            Activated += MainWindow_Activated;

            Trace.Listeners.Clear();
            Trace.Listeners.Add(new LocalTraceListener(OnMessage));

            Closing += Window_Closing;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
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

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            App.Register(this);
        }

        int ticks = 0;
        public void Tick()
        {
            // TODO: Bad solution to too much work being done.
            ticks++;
            if (ticks % 10 == 0) _viewModel.Tick();
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
    }
}
