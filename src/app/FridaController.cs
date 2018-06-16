using Frida;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;

namespace GTAPilot
{
    internal class FridaController : INotifyPropertyChanged
    {
        public event Action<string> OnMessage;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsConnected { get; private set; }

        private Dispatcher _fridaDispatcher;
        private Session _session;
        private Script _script;

        public FridaController()
        {
            new Thread(() =>
            {
                _fridaDispatcher = Dispatcher.CurrentDispatcher;
                var mgr = new DeviceManager(_fridaDispatcher);

                var devices = mgr.EnumerateDevices();
                if (devices == null || devices.Length == 0)
                {
                    TraceLine($"EnumerateDevices: {devices}");
                    return;
                }

                // TODO: handle app restarting at runtime
                var apps = System.Diagnostics.Process.GetProcessesByName("xboxapp");
                if (apps.Length == 0)
                {
                    TraceLine($"EnumerateDevices: {apps}");
                    return;
                }

                _session = devices[0].Attach((uint)apps[0].Id);
                _session.Detached += Session_Detached;

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GTAPilot.Properties.XboxApp.js"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    _script = _session.CreateScript(reader.ReadToEnd());
                    _script.Message += Script_Message;

                    _script.Load();

                    TraceLine($"Connected to {_session.Pid}");
                    IsConnected = true;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));

                    Dispatcher.Run();
                }
            }).Start();
        }

        private void Script_Message(object sender, ScriptMessageEventArgs e)
        {
            TraceLine(e.Message);
            OnMessage?.Invoke(e.Message);
        }

        private void Session_Detached(object sender, SessionDetachedEventArgs e)
        {
            TraceLine("Session Detached.");
            IsConnected = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
        }

        public void SendMessage(string message)
        {
            _fridaDispatcher.BeginInvoke((Action)(() =>
                {
                    _script.Post(message);
                }));
        }

        private void TraceLine(string msg)
        {
            System.Diagnostics.Trace.WriteLine($"FRIDA: {msg}");
        }
    }
}