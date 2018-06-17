using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GTAPilot
{
    public class FlightController
    {
        [DataContract]
        public class ControllerMessage
        {
            /*
            [DataMember]public int DPAD_UP { get; set; }
            [DataMember]public int DPAD_DOWN { get; set; }
            [DataMember]public int DPAD_LEFT { get; set; }
            [DataMember]public int DPAD_RIGHT { get; set; }
            [DataMember]public int START { get; set; }
            [DataMember]public int BACK { get; set; }
            [DataMember]public int LEFT_THUMB { get; set; }
            [DataMember]public int RIGHT_THUMB { get; set; }
            [DataMember]public int LEFT_SHOULDER { get; set; }
            [DataMember]public int RIGHT_SHOULDER { get; set; }
            [DataMember]public int A { get; set; }
            [DataMember]public int B { get; set; }
            [DataMember]public int X { get; set; }
            [DataMember]public int Y { get; set; }
            */

            [DataMember] public int Buttons { get; set; }
            [DataMember]public int RIGHT_TRIGGER { get; set; }
            [DataMember]public int LEFT_TRIGGER { get; set; }
            [DataMember]public int RIGHT_THUMB_X { get; set; }
            [DataMember]public int RIGHT_THUMB_Y { get; set; }
            [DataMember]public int LEFT_THUMB_X { get; set; }
            [DataMember]public int LEFT_THUMB_Y { get; set; }
        }


        private DataContractJsonSerializer _deserializer = new DataContractJsonSerializer(typeof(ControllerMessage));

        FridaController _controller;

        internal FlightController(FridaController fridaController)
        {
            _controller = fridaController;

            fridaController.OnMessage += FridaController_OnMessage;
        }

        private void FridaController_OnMessage(string payload)
        {
            var frameId = Timeline.LastFrameId;

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(payload)))
            {
                ms.Position = 0;
                var msg = (ControllerMessage)_deserializer.ReadObject(ms);

                SetValueAndHistory(frameId, (id) => Timeline.Data[id].Roll, msg.LEFT_THUMB_X + ushort.MaxValue/2);
                SetValueAndHistory(frameId, (id) => Timeline.Data[id].Pitch, msg.LEFT_THUMB_Y + ushort.MaxValue/2);
                SetValueAndHistory(frameId, (id) => Timeline.Data[id].Speed, msg.RIGHT_TRIGGER);
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

        public void ToggleLandingGear()
        {
            Trace.WriteLine("Toggle landing gear");
            SendMessage("{\"LEFT_THUMB\":\"0\"}");
        }

        public void LockViewMin()
        {
            SendMessage("{\"RIGHT_THUMB_Y\":\"-17700\",\"RIGHT_THUMB_X\":\"1\"}");
        }

        public void ResetFlaps()
        {
            SendMessage("{\"LEFT_TRIGGER\":\"20\"}");
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

        internal void SetFlaps(int value)
        {
            // flaps max = 235, past is engine shutoff
            SendMessage("{\"LEFT_TRIGGER\":\"" + value + "\"}");
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

        public void SetRoll(double value, int ticks = 12)
        {
            SendMessage("{\"LEFT_THUMB_X\":\"" + value + "\"}");
        }

        public void SetPitch(double value, int ticks = 12)
        {
            SendMessage("{\"LEFT_THUMB_Y\":\"" + value + "\"}");
        }

        public void SetThrottle(double value, int ticks = 20)
        {
            SendMessage("{\"RIGHT_TRIGGER\":\"" + (int)value + "\"}");
        }

        public void SetLeftRudder(double ticks)
        {
            SendMessage("{\"LEFT_SHOULDER\":\"" + ticks + "\"}");
            SendMessage("{\"RIGHT_SHOULDER\":\"0\"}");
        }

        public void SetRightRudder(double ticks)
        {
            SendMessage("{\"RIGHT_SHOULDER\":\"" + ticks + "\"}");
            SendMessage("{\"LEFT_SHOULDER\":\"0\"}");
        }
    }
}
