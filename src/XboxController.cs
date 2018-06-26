using GTAPilot.Interop;
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
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<XINPUT_GAMEPAD_BUTTONS> ButtonPressed;
        public event EventHandler<ControllerMessage> ControllerInput;

        public FpsCounter XInput_In { get; }
        public FpsCounter XInput_Out => _controller.Counter;

        [DataContract]
        public class BaseMessage
        {
            [DataMember] public string Type { get; set; }
            [DataMember] public int Value { get; set; }
        }

        [DataContract]
        public class ControllerMessage
        {
            [DataMember] public XINPUT_GAMEPAD_BUTTONS Buttons { get; set; }
            [DataMember] public int RIGHT_TRIGGER { get; set; }
            [DataMember] public int LEFT_TRIGGER { get; set; }
            [DataMember] public int RIGHT_THUMB_X { get; set; }
            [DataMember] public int RIGHT_THUMB_Y { get; set; }
            [DataMember] public int LEFT_THUMB_X { get; set; }
            [DataMember] public int LEFT_THUMB_Y { get; set; }
        }

        private DataContractJsonSerializer _baseDeserializer = new DataContractJsonSerializer(typeof(BaseMessage));
        private DataContractJsonSerializer _controlInputdeserializer = new DataContractJsonSerializer(typeof(ControllerMessage));
        private FridaAppConnector _controller;

        internal XboxController(FridaAppConnector fridaController)
        {
            _controller = fridaController;

            fridaController.OnMessage += FridaController_OnMessage;
            XInput_In = new FpsCounter();
        }

        private void FridaController_OnMessage(string payload)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(payload)))
            {
                ms.Position = 0;
                var msg = (BaseMessage)_baseDeserializer.ReadObject(ms);

                switch (msg.Type)
                {
                    case "XInputFPS":
                        XInput_In.Fps = msg.Value;
                        break;
                    case "InputData":
                        ms.Position = 0;
                        ControllerInput?.Invoke(this, (ControllerMessage)_controlInputdeserializer.ReadObject(ms));
                        break;
                    case "ButtonPress":
                        ButtonPressed?.Invoke(this, (XINPUT_GAMEPAD_BUTTONS)msg.Value);
                        break;
                }
            }
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

        public void SetLeftThumbX(double value)
        {
            SendMessage("{\"LEFT_THUMB_X\":\"" + value + "\"}");
        }

        public void SetLeftThumbY(double value)
        {
            SendMessage("{\"LEFT_THUMB_Y\":\"" + value + "\"}");
        }

        public void SetRightTrigger(double value)
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
