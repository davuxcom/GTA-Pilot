using GTAPilot.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace GTAPilot
{
    class XboxApp : INotifyPropertyChanged, IFrameProducer
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<int, Bitmap> FrameProduced;

        public IntPtr WindowHandle { get; private set; }
        public bool IsConnected { get; private set; }
        public XboxController Controller { get; private set; }
        public FpsCounter Capture { get; private set; }

        private DesktopFrameProducer _desktopFrameProducer;
        private FridaAppConnector _fridaConnector;

        public XboxApp()
        {
            _fridaConnector = new FridaAppConnector();
            _fridaConnector.PropertyChanged += FridaAppConnector_PropertyChanged;
            Controller = new XboxController(_fridaConnector);
            Capture = new FpsCounter();

            WindowHandle = GetWindow();
            if (WindowHandle != IntPtr.Zero)
            {
                ConnectAsync();
            }

            // TODO: Listen for window changes and connect/disconnect
        }

        private void ConnectAsync()
        {
            _fridaConnector.ConnectAsync((uint)Process.GetProcessesByName("xboxapp")[0].Id, GetScriptContent());

            if (_desktopFrameProducer != null)
            {
                _desktopFrameProducer.FrameProduced -= DesktopFrameProducer_FrameProduced;
                _desktopFrameProducer = null;
            }

            // TODO: Find screen from window location
            _desktopFrameProducer = new DesktopFrameProducer(GetWindow());
            _desktopFrameProducer.FrameProduced += DesktopFrameProducer_FrameProduced;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Controller)));
        }

        private void FridaAppConnector_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_fridaConnector.IsConnected))
            {
                if (_fridaConnector.IsConnected)
                {
                    IsConnected = true;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
                }
                else
                {
                    // TODO: reconnect logic

                    IsConnected = false;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
                }
            }
        }

        private void DesktopFrameProducer_FrameProduced(int id, Bitmap frame)
        {
            FrameProduced?.Invoke(id, frame);
            Capture.GotFrame();
        }

        private IntPtr GetWindow() => User32.FindWindow("ApplicationFrameWindow", "Xbox");

        private static string GetScriptContent()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GTAPilot.XboxApp.js"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public void Begin()
        {
            _desktopFrameProducer.Begin();
        }

        public void Stop()
        {
            _desktopFrameProducer.Stop();
        }
    }
}
