using System.Diagnostics;

namespace GTAPilot
{
    class SystemManager
    {
        public static SystemManager Instance = null;

        public FlightPlan FlightPlan => _computer.FlightPlan;
        public IndicatorHandler IndicatorHost;
        public ModeControlPanel MCP = new ModeControlPanel();

        XboxApp _app;
        FrameInputCoordinator _coordinator;
        FlightDataComputer _computer;

        public SystemManager(string flightPlanFile)
        {
            Instance = this;

            _app = new XboxApp();
            _coordinator = new FrameInputCoordinator(_app, f => IndicatorHost.HandleFrameArrived(f));

            _app.Controller.ButtonPressed += Controler_ButtonPressed;
            _app.PropertyChanged += XboxApp_PropertyChanged;

            _computer = new FlightDataComputer(MCP, _app.Controller, flightPlanFile);
            IndicatorHost = new IndicatorHandler(_computer);

            _coordinator.Begin();
            Timeline.Begin();
        }

        private void Controler_ButtonPressed(object sender, XboxController.XInputButtons e)
        {
            switch (e)
            {
                case XboxController.XInputButtons.BACK:
                    if (MCP.BankHold || MCP.HeadingHold || MCP.VSHold || MCP.AltitudeHold || MCP.IASHold | MCP.LNAV)
                    {
                        MCP.BankHold = false;
                        MCP.HeadingHold = false;
                        MCP.VSHold = false;
                        MCP.AltitudeHold = false;
                        MCP.IASHold = false;
                        MCP.LNAV = false;
                    }
                    else
                    {
                        MCP.VSHold = true;
                        MCP.LNAV = true;
                    }
                    break;
            }

            Trace.WriteLine($"Button Pressed: {e}");
        }

        private void XboxApp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // crappy, won't work for reconnect
            if (e.PropertyName == "IsConnected")
            {
                _app.Controller.HoldRightThumbY();
            }
        }

        internal void StopCapture()
        {
            _app.Stop();
        }
    }
}
