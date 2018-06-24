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

        private DesktopFrameProducer _desktopFrameProducer;

        public XboxApp()
        {
            WindowHandle = GetWindow();
            if (WindowHandle != IntPtr.Zero)
            {
                Connect();
            }

            // TODO: Listen for window changes and connect/disconnect
        }

        private void Connect()
        {
            Controller = new XboxController(new FridaController((uint)Process.GetProcessesByName("xboxapp")[0].Id, GetScriptContent()));
            IsConnected = true;

            // TODO: bad place for this
            Controller.HoldRightThumbY();

            // TODO: Find screen from window location

            if (_desktopFrameProducer != null)
            {
                _desktopFrameProducer.FrameProduced -= DesktopFrameProducer_FrameProduced;
                _desktopFrameProducer = null;
            }

            _desktopFrameProducer = new DesktopFrameProducer(1);
            _desktopFrameProducer.FrameProduced += DesktopFrameProducer_FrameProduced;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Controller)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
        }

        private void DesktopFrameProducer_FrameProduced(int id, Bitmap frame)
        {
            FrameProduced?.Invoke(id, frame);
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
