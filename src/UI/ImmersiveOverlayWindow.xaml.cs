using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GTAPilot
{
    public partial class ImmersiveOverlayWindow : Window
    {
        class LocalTraceListener : TraceListener
        {
            Action<string> _handler;

            public LocalTraceListener(Action<string> handler) => _handler = handler;

            public override void Write(string message) => WriteLine(message);
            public override void WriteLine(string message) => _handler(message);
        }

        public RelayCommand FlightPlanOpen { get; }
        public RelayCommand FlightPlanSave { get; }
        public RelayCommand FlightPlanEdit { get; }
        public RelayCommand OpenRecorder { get; }
        public RelayCommand SaveData { get; }

        public ImmersiveOverlayWindow()
        {
            InitializeComponent();

            Trace.Listeners.Clear();
            Trace.Listeners.Add(new LocalTraceListener(OnMessage));

            var plan = SystemManager.Instance.FlightPlan;
            FlightPlanEdit = new RelayCommand(() =>
            {
                var editor = new FlightPlanMap(plan.Points.ToArray());
                editor.ShowDialog();

                plan.Load(editor.Points);
            });

            FlightPlanSave = new RelayCommand(() =>
            {
                StringBuilder ret = new StringBuilder();
                foreach (var p in plan.Points)
                {
                    ret.AppendLine($"{p.X},{p.Y}");
                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "FlightPlan";
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";

                if (dlg.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dlg.FileName, ret.ToString());
                }
            });

            FlightPlanOpen = new RelayCommand(() =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = "FlightPlan";
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";

                if (dlg.ShowDialog() == true)
                {
                    SystemManager.Instance.FlightPlan.LoadFromFile(dlg.FileName);
                }
            });

            OpenRecorder = new RelayCommand(() =>
            {

                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.SelectedPath = @"c:\save\recording3";
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                    SystemManager.Instance.StartRecording(dialog.SelectedPath);

                    var rc = new RecorderWindow();
                    rc.Show();
                }

            });

            SaveData = new RelayCommand(() =>
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "Run";
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";

                if (dlg.ShowDialog() == true)
                {
                    Timeline.Save(dlg.FileName);
                }
            });

            DataContext = this;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            lstLog.Visibility = lstLog.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MenuItem2_Click(object sender, RoutedEventArgs e)
        {
            AnalyzerWindow.Raise();
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
    }
}
