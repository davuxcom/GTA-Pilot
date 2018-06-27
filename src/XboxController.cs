using GTAPilot.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GTAPilot
{
    public class XboxController
    {
        [DataContract]
        class BaseMessage
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

        public event EventHandler<XINPUT_GAMEPAD_BUTTONS> ButtonPressed;
        public event EventHandler<ControllerMessage> ControllerInput;

        public FpsCounter XInput_In { get; }
        public FpsCounter XInput_Out { get; }

        private DataContractJsonSerializer _baseDeserializer = new DataContractJsonSerializer(typeof(BaseMessage));
        private DataContractJsonSerializer _controlInputdeserializer = new DataContractJsonSerializer(typeof(ControllerMessage));
        private FridaAppConnector _controller;
        private Dictionary<XINPUT_GAMEPAD_AXIS, int> _nextAxis = new Dictionary<XINPUT_GAMEPAD_AXIS, int>();
        private Dictionary<XINPUT_GAMEPAD_BUTTONS, int> _nextButtons = new Dictionary<XINPUT_GAMEPAD_BUTTONS, int>();
        private object _outputlock = new object();

        internal XboxController(FridaAppConnector fridaController)
        {
            _controller = fridaController;

            fridaController.MessageReceived += FridaController_OnMessage;
            fridaController.MessageSent += FridaController_MessageSent;
            XInput_In = new FpsCounter();
            XInput_Out = new FpsCounter();
        }

        private void FridaController_MessageSent()
        {
            XInput_Out.GotFrame();
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

        public void Press(XINPUT_GAMEPAD_BUTTONS btn, int ticks = 0)
        {
            lock (_outputlock)
            {
                _nextButtons[btn] = ticks;

                // Zero opposite shoulder button
                if (btn == XINPUT_GAMEPAD_BUTTONS.RIGHT_SHOULDER)
                {
                    _nextButtons[XINPUT_GAMEPAD_BUTTONS.LEFT_SHOULDER] = 0;
                }
                else if (btn == XINPUT_GAMEPAD_BUTTONS.LEFT_SHOULDER)
                {
                    _nextButtons[XINPUT_GAMEPAD_BUTTONS.RIGHT_SHOULDER] = 0;
                }
            }
        }

        // LEFT_TRIGGER max = 235, past is engine shutoff
        public void Set(XINPUT_GAMEPAD_AXIS axis, int value)
        {
            lock (_outputlock)
            {
                _nextAxis[axis] = value;
            }
        }

        public void Flush()
        {
            string msg = "{";

            lock (_outputlock)
            {
                if (_nextButtons.Count == 0 && _nextAxis.Count == 0) return;

                foreach (var a in _nextAxis) msg += $"\"{a.Key.ToString()}\": {a.Value},";
                foreach (var a in _nextButtons) msg += $"\"{a.Key.ToString()}\": {a.Value},";

                _nextAxis.Clear();
                _nextButtons.Clear();
            }

            msg = msg.TrimEnd(',') + "}";
            _controller.SendMessage(msg);
        }
    }
}
