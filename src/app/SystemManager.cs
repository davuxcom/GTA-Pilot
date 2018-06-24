using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;

namespace GTAPilot
{
    enum FpsCounterType
    {
        Capture_Enqueue,
        Capture_Dequeue,

        Roll,
        Pitch,
        Yaw,
        Altitude,
        Airspeed,

        XInput,
    }


    class SystemManager
    {
        public static SystemManager Instance = null;

        public FlightPlan FlightPlan => _computer.FlightPlan;

        IFrameProducer _producer;
        FrameInputCoordinator _coordinator;
        public IndicatorHost IndicatorHost;
        FlightController _control;
        public ModeControlPanel MCP = new ModeControlPanel();
        FlightDataComputer _computer;

        public SystemManager(IFrameProducer producer, string flightPlanFile = null, FridaController fridaController = null)
        {
            Instance = this;

            _producer = producer;
            _coordinator = new FrameInputCoordinator(producer, FrameArrived);

            if (fridaController != null)
            {
                _control = new FlightController(fridaController);
                _control.ButtonPressed += Controler_ButtonPressed;
                fridaController.PropertyChanged += FridaController_PropertyChanged;
            }

            _computer = new FlightDataComputer(MCP, _control, flightPlanFile);
            IndicatorHost = new IndicatorHost(_computer);

            _coordinator.Begin();
            Timeline.Begin();
        }

        private void Controler_ButtonPressed(object sender, FlightController.XInputButtons e)
        {
            switch(e)
            {
                case FlightController.XInputButtons.BACK:
                    if (MCP.BankHold || MCP.HeadingHold || MCP.VSHold || MCP.AltitudeHold || MCP.SpeedHold)
                    {
                        MCP.BankHold = false;
                        MCP.HeadingHold = false;
                        MCP.VSHold = false;
                        MCP.AltitudeHold = false;
                        MCP.SpeedHold = false;
                    }
                    else
                    {
                        MCP.VSHold = true;
                        MCP.HeadingHold = true;
                    }
                    break;
            }

            Trace.WriteLine($"Button Pressed: {e}");
        }

        public SystemManager(IFrameProducer producer, Action<FrameData> consumer, FridaController fridaController = null)
        {
            _producer = producer;
            _coordinator = new FrameInputCoordinator(producer, consumer);

            if (fridaController != null)
            {
                _control = new FlightController(fridaController);
                _control.ButtonPressed += Controler_ButtonPressed;
                fridaController.PropertyChanged += FridaController_PropertyChanged;
            }

            _coordinator.Begin();
            Timeline.Begin();
        }

        public static void InitializeForLiveCapture()
        {
            var fridaController = new FridaController((uint)Process.GetProcessesByName("xboxapp")[0].Id, GetScriptContent());
            var mgr = new SystemManager(new DesktopFrameProducer(2), @"C:\workspace\FlightPlan.txt", fridaController);
        }

        private static string GetScriptContent()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GTAPilot.XboxApp.js"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private void FridaController_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // crappy, won't work for reconnect
            if (e.PropertyName == "IsConnected")
            {
                _control.LockViewMin();
            }
        }

        internal FpsCounter GetCounter(FpsCounterType type)
        {
            switch(type)
            {
                case FpsCounterType.Capture_Enqueue: return _coordinator.EnqueuePerf;
                case FpsCounterType.Capture_Dequeue: return _coordinator.DequeuePerf;
                case FpsCounterType.Roll: return IndicatorHost.Roll.Counter;
                case FpsCounterType.Pitch: return IndicatorHost.Pitch.Counter;
                case FpsCounterType.Airspeed: return IndicatorHost.Airspeed.Counter;
                case FpsCounterType.Altitude: return IndicatorHost.Altitude.Counter;
                case FpsCounterType.Yaw: return IndicatorHost.Compass.Counter;
                case FpsCounterType.XInput: return _control.XInputFPS;
            }
            throw new NotImplementedException();
        }

        internal Bitmap GetLatestFrame(FpsCounterType type)
        {
            switch(type)
            {
                case FpsCounterType.Roll: return IndicatorHost.Roll.Image?.ToBitmap();
                case FpsCounterType.Pitch: return IndicatorHost.Pitch.Image?.ToBitmap();
                case FpsCounterType.Airspeed: return IndicatorHost.Airspeed.Image?.ToBitmap();
                case FpsCounterType.Altitude: return IndicatorHost.Altitude.Image?.ToBitmap();
                case FpsCounterType.Yaw: return IndicatorHost.Compass.Image?.ToBitmap();
            }
            throw new NotImplementedException();
        }

        private void FrameArrived(FrameData data)
        {
            IndicatorHost.HandleFrameArrived(data);
        }

        internal void StopCapture()
        {
            _producer.Stop();
        }
    }
}
