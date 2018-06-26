using System.Diagnostics;

namespace GTAPilot
{
    class SystemManager
    {
        public static SystemManager Instance = null;

        public FlightPlan FlightPlan { get; }
        public IndicatorHandler IndicatorHost { get; }
        public ModeControlPanel MCP { get; }
        public XboxApp App { get; }

        public FpsCounter Capture = new FpsCounter();

        FlightDataComputer _computer;

        public SystemManager()
        {
            Instance = this;

            FlightPlan = new FlightPlan();
            FlightPlan.LoadFromFile(@"c:\workspace\FlightPlan.txt");
            MCP = new ModeControlPanel();

            App = new XboxApp();
            App.FrameProduced += XboxApp_FrameProduced;
            App.Controller.ButtonPressed += Controler_ButtonPressed;
            App.PropertyChanged += XboxApp_PropertyChanged;

            _computer = new FlightDataComputer(MCP, App.Controller, FlightPlan);
            IndicatorHost = new IndicatorHandler(_computer);

            Timeline.Begin();
        }

        private void XboxApp_FrameProduced(int frameId, System.Drawing.Bitmap frame)
        {
            IndicatorHost.HandleFrameArrived(new FrameData(frameId, frame, Timeline.Duration.Elapsed.TotalSeconds));

            Capture.GotFrame();
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
            if (e.PropertyName == "IsConnected")
            {
                App.Controller.HoldRightThumbY();
            }
        }

        internal void StopCapture()
        {
            App.Stop();
        }
    }
}
