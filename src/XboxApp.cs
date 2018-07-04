using GTAPilot.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace GTAPilot
{
    // XboxApp locates the window/app and maintains the Frida connection.
    // We capture frames from the display containing the window, and 
    // send/receive controller input.
    class XboxApp : INotifyPropertyChanged, IFrameProducer
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<int, Bitmap> FrameProduced;

        public bool IsConnected { get; private set; }
        public bool IsRunning => WindowHandle != IntPtr.Zero;
        public IntPtr WindowHandle { get; }
        public XboxController Controller { get; }

        private static readonly string s_scriptResourceName = "GTAPilot.XboxApp.js";
        private DesktopFrameProducer _desktopFrameProducer;
        private FridaAppConnector _fridaConnector;

        public XboxApp()
        {
            _fridaConnector = new FridaAppConnector();
            _fridaConnector.PropertyChanged += FridaAppConnector_PropertyChanged;
            Controller = new XboxController(_fridaConnector);
            WindowHandle = GetWindow();

            if (IsRunning) ConnectAsync();
        }

        public void Begin()
        {
            _desktopFrameProducer.Begin();
        }

        public void Stop()
        {
            _desktopFrameProducer.Stop();
        }

        private void ConnectAsync()
        {
            _fridaConnector.ConnectAsync((uint)Process.GetProcessesByName("xboxapp")[0].Id, GetScriptContent());

            Debug.Assert(_desktopFrameProducer == null);

            _desktopFrameProducer = new DesktopFrameProducer(GetWindow());
            _desktopFrameProducer.FrameProduced += DesktopFrameProducer_FrameProduced;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Controller)));
        }

        private void FridaAppConnector_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_fridaConnector.IsConnected))
            {
                IsConnected = _fridaConnector.IsConnected;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
            }
        }

        private void DesktopFrameProducer_FrameProduced(int id, Bitmap frame)
        {
            FrameProduced?.Invoke(id, frame);
        }

        public static IntPtr GetWindow() => User32.FindWindow("ApplicationFrameWindow", "Xbox");

        private static string GetScriptContent()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(s_scriptResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
