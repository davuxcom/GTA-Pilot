using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GTAPilot
{
    public class XboxController : INotifyPropertyChanged
    {
        public FpsCounter XInputFPS { get; private set; }
        public event EventHandler<XInputButtons> ButtonPressed;

        [Flags]
        public enum XInputButtons : int
        {
            DPAD_UP = 0x0001,
            DPAD_DOWN = 0x0002,
            DPAD_LEFT = 0x0004,
            DPAD_RIGHT = 0x0008,
            START = 0x0010,
            BACK = 0x0020,
            LEFT_THUMB = 0x0040,
            RIGHT_THUMB = 0x0080,
            LEFT_SHOULDER = 0x0100,
            RIGHT_SHOULDER = 0x0200,
            A = 0x1000,
            B = 0x2000,
            X = 0x4000,
            Y = 0x8000,
        };

        [DataContract]
        public class BaseMessage
        {
            [DataMember] public string Type { get; set; }
            [DataMember] public int Value { get; set; }
        }

        [DataContract]
        public class ControllerMessage
        {
            [DataMember] public XInputButtons Buttons { get; set; }
            [DataMember] public int RIGHT_TRIGGER { get; set; }
            [DataMember] public int LEFT_TRIGGER { get; set; }
            [DataMember] public int RIGHT_THUMB_X { get; set; }
            [DataMember] public int RIGHT_THUMB_Y { get; set; }
            [DataMember] public int LEFT_THUMB_X { get; set; }
            [DataMember] public int LEFT_THUMB_Y { get; set; }
        }

        private DataContractJsonSerializer _baseDeserializer = new DataContractJsonSerializer(typeof(BaseMessage));
        private DataContractJsonSerializer _controlInputdeserializer = new DataContractJsonSerializer(typeof(ControllerMessage));
        private FridaController _controller;

        public event PropertyChangedEventHandler PropertyChanged;

        internal XboxController(FridaController fridaController)
        {
            _controller = fridaController;

            fridaController.OnMessage += FridaController_OnMessage;
            XInputFPS = new FpsCounter();
        }

        private void FridaController_OnMessage(string payload)
        {
            var frameId = Timeline.LastFrameId;

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(payload)))
            {
                ms.Position = 0;
                var msg = (BaseMessage)_baseDeserializer.ReadObject(ms);

                switch(msg.Type)
                {
                    case "XInputFPS":
                        XInputFPS.Fps = msg.Value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(XInputFPS)));
                        break;
                    case "InputData":
                        {
                            ms.Position = 0;
                            var controllerMsg = (ControllerMessage)_controlInputdeserializer.ReadObject(ms);

                            SetValueAndHistory(frameId, (id) => Timeline.Data[id].Roll, controllerMsg.LEFT_THUMB_X + ushort.MaxValue / 2);
                            SetValueAndHistory(frameId, (id) => Timeline.Data[id].Pitch, controllerMsg.LEFT_THUMB_Y + ushort.MaxValue / 2);
                            SetValueAndHistory(frameId, (id) => Timeline.Data[id].Speed, controllerMsg.RIGHT_TRIGGER);
                        }
                        break;
                    case "ButtonPress":
                        ButtonPressed?.Invoke(this, (XInputButtons)msg.Value);
                        break;
                }
            }
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

        public void PressLeftThumb()
        {
            Trace.WriteLine("Toggle landing gear");
            SendMessage("{\"LEFT_THUMB\":\"0\"}");
        }

        public void HoldRightThumbY(int value = -17700)
        {
            SendMessage("{\"RIGHT_THUMB_Y\":\"" + value + "\",\"RIGHT_THUMB_X\":\"0\"}");
        }



        private void SendMessage(string msg)
        {
            _controller.SendMessage(msg);
        }

        internal void PressA()
        {
            Trace.WriteLine("Toggle A");
            SendMessage("{\"A\":\"0\"}");
        }

        internal void HoldLefTrigger(int value)
        {
            // flaps max = 235, past is engine shutoff
            SendMessage("{\"LEFT_TRIGGER\":\"" + value + "\"}");
        }

        public void ResetLeftTrigger()
        {
            SendMessage("{\"LEFT_TRIGGER\":\"20\"}");
        }

        internal void PressB()
        {
            Trace.WriteLine("Toggle B");
            SendMessage("{\"B\":\"0\"}");
        }

        internal void PressStart()
        {
            Trace.WriteLine("Toggle Start");
            SendMessage("{\"START\":\"0\"}");
        }

        public void SetLeftThumbX(double value, int ticks = 12)
        {
            SendMessage("{\"LEFT_THUMB_X\":\"" + value + "\"}");
        }

        public void SetLeftThumbY(double value, int ticks = 12)
        {
            SendMessage("{\"LEFT_THUMB_Y\":\"" + value + "\"}");
        }

        public void SetRightTrigger(double value, int ticks = 20)
        {
            SendMessage("{\"RIGHT_TRIGGER\":\"" + (int)value + "\"}");
        }

        public void SetLeftShoulder(double ticks)
        {
            SendMessage("{\"LEFT_SHOULDER\":\"" + ticks + "\", \"RIGHT_SHOULDER\":\"0\"}");
        }

        public void SetRightShoulder(double ticks)
        {
            SendMessage("{\"RIGHT_SHOULDER\":\"" + ticks + "\", \"LEFT_SHOULDER\":\"0\"}");
        }
    }
}
