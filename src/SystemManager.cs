using GTAPilot.Interop;
using System;
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
        public FpsCounter Capture { get; }
        public bool IsReplay => _replay != null;

        private FlightDataComputer _computer;
        private ReplayFrameProducer _replay;

        public SystemManager()
        {
            Instance = this;

            Capture = new FpsCounter();
            FlightPlan = new FlightPlan();
            FlightPlan.LoadFromFile(@"c:\workspace\FlightPlan.txt");
            MCP = new ModeControlPanel();

            App = new XboxApp();

            _computer = new FlightDataComputer(MCP, App.Controller, FlightPlan);
            IndicatorHost = new IndicatorHandler(_computer);
            Timeline.Begin();

            if (App.IsRunning)
            {
                App.FrameProduced += XboxApp_FrameProduced;
                App.Controller.ButtonPressed += Controler_ButtonPressed;
                App.Controller.ControllerInput += Controller_ControllerInput;
                App.PropertyChanged += XboxApp_PropertyChanged;
                App.Begin();
            }
            else
            {
                _replay = new ReplayFrameProducer(@"C:\save\recording1");
                _replay.FrameProduced += XboxApp_FrameProduced;
                _replay.Begin();
            }
        }

        private void Controller_ControllerInput(object sender, XboxController.ControllerMessage controllerMsg)
        {
            var frameId = Timeline.LatestFrameId;

            SetValueAndHistory(frameId, (id) => Timeline.Data[id].Roll, controllerMsg.LEFT_THUMB_X + ushort.MaxValue / 2);
            SetValueAndHistory(frameId, (id) => Timeline.Data[id].Pitch, controllerMsg.LEFT_THUMB_Y + ushort.MaxValue / 2);
            SetValueAndHistory(frameId, (id) => Timeline.Data[id].Speed, controllerMsg.RIGHT_TRIGGER);
        }

        private void SetValueAndHistory(int frameId, Func<int, TimelineValue> getFrame, double value)
        {
            var thisFrame = getFrame(frameId);

            do
            {
                thisFrame.InputValue = value;
                thisFrame = getFrame(--frameId);
            } while (frameId > 1 && double.IsNaN(thisFrame.InputValue));
        }

        private void XboxApp_FrameProduced(int frameId, System.Drawing.Bitmap frame)
        {
            IndicatorHost.HandleFrameArrived(new FrameData(frameId, frame, Timeline.Duration.Elapsed.TotalSeconds));
            Capture.GotFrame();
        }

        private void Controler_ButtonPressed(object sender, XINPUT_GAMEPAD_BUTTONS e)
        {
            switch (e)
            {
                case XINPUT_GAMEPAD_BUTTONS.BACK:
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
