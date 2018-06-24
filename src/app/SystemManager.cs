using System;
using System.Diagnostics;

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

        public SystemManager(XboxApp app, string flightPlanFile)
        {
            Instance = this;

            _producer = app;
            _coordinator = new FrameInputCoordinator(app, f => IndicatorHost.HandleFrameArrived(f));

            app.Controller.ButtonPressed += Controler_ButtonPressed;
            app.PropertyChanged += XboxApp_PropertyChanged;

            _computer = new FlightDataComputer(MCP, _control, flightPlanFile);
            IndicatorHost = new IndicatorHost(_computer);

            _coordinator.Begin();
            Timeline.Begin();
        }

        private void Controler_ButtonPressed(object sender, FlightController.XInputButtons e)
        {
            switch (e)
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
                fridaController.PropertyChanged += XboxApp_PropertyChanged;
            }

            _coordinator.Begin();
            Timeline.Begin();
        }

        public static void InitializeForLiveCapture()
        {
            new SystemManager(new XboxApp(), @"C:\workspace\FlightPlan.txt");
        }

        private void XboxApp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // crappy, won't work for reconnect
            if (e.PropertyName == "IsConnected")
            {
                _control.LockViewMin();
            }
        }

        internal void StopCapture()
        {
            _producer.Stop();
        }
    }
}
