using System.Diagnostics;
using System.Net.Sockets;

namespace GTAPilot
{
    public class FlightController
    {
        public bool IsAuto { get; set; }
        public bool SuspendRudder { get; internal set; }

        public void ToggleLandingGear()
        {
            Trace.WriteLine("Toggle landing gear");
            SendMessage("{\"lthumb\":\"0\"}");
        }

        public void LockViewMin()
        {
            SendMessage("{\"thumbry\":\"-17700\",\"thumbry_ticks\":\"5000000\",\"thumbrx\":\"0\",\"thumbrx_ticks\":\"5000000\"}", true);
            // SendMessage("{\"thumbry\":\"-32000\",\"thumbry_ticks\":\"5000000\",\"thumbrx\":\"0\",\"thumbrx_ticks\":\"5000000\"}", true);
            // SendMessage("{\"thumbry\":\"-32000\",\"thumbry_ticks\":\"5000000\",\"thumbrx\":\"0\",\"thumbrx_ticks\":\"5000000\"}", true);
        }

        public void LockViewFull()
        {
            SendMessage("{\"thumbry\":\"-32000\",\"thumbry_ticks\":\"5000000\",\"thumbrx\":\"0\",\"thumbrx_ticks\":\"5000000\"}", true);
            // SendMessage("{\"thumbry\":\"-32000\",\"thumbry_ticks\":\"5000000\",\"thumbrx\":\"0\",\"thumbrx_ticks\":\"5000000\"}", true);
        }

        public void ReleaseView()
        {
            SendMessage("{\"thumbry\":\"0\",\"thumbry_ticks\":\"1\",\"thumbrx\":\"0\",\"thumbrx_ticks\":\"1\"}", true);
        }

        public void ResetFlaps()
        {
            SendMessage("{\"ltrigger\":\"0\",\"lt_ticks\":\"20\"}", true);

        }

        static TcpClient tcp;

        private void SendMessage(string msg, bool forceSend = false)
        {
            if (!IsAuto && !forceSend) return;

            if (tcp == null)
            {
                tcp = new TcpClient("localhost", 3377);
            }

            var s = tcp.GetStream();

            var bytes = System.Text.Encoding.UTF8.GetBytes(msg + "\n");

            s.Write(bytes, 0, bytes.Length);
            s.Flush();

            /*

            Stopwatch w = new Stopwatch();
            w.Start();
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "xbox", PipeDirection.Out))
            {
                pipeClient.Connect(1000);
                using (StreamWriter sw = new StreamWriter(pipeClient))
                {
                    sw.WriteLine(msg);
                    sw.Flush();
                    sw.Close();
                }
                pipeClient.Close();
            }
            w.Stop();
            //  Trace.WriteLine("%%%%%%%%% " + w.ElapsedMilliseconds);
            */
        }

        internal void PressA()
        {
            Trace.WriteLine("Toggle A");
            SendMessage("{\"a\":\"0\"}");
        }

        internal void SetFlaps(int value)
        {

            // flaps max = 235, past is engine shutoff
            SendMessage("{\"ltrigger\":\"" + value + "\",\"lt_ticks\":\"10000\"}");
        }

        internal void PressB()
        {
            Trace.WriteLine("Toggle B");
            SendMessage("{\"b\":\"0\"}");
        }

        internal void PressStart()
        {
            Trace.WriteLine("Toggle Start");
            SendMessage("{\"start\":\"0\",\"lbumper\":\"0\",\"lbumper_ticks\":\"0\",\"rbumper\":\"0\",\"rbumper_ticks\":\"0\"}");
        }

        public void SetRoll(double value, int ticks = 12)
        {
            SendMessage("{\"thumblx\":\"" + value + "\",\"thumblx_ticks\":\"" + ticks + "\"}");
        }

        public void SetPitch(double value, int ticks = 12)
        {
            SendMessage("{\"thumbly\":\"" + value + "\",\"thumbly_ticks\":\"" + ticks + "\"}");
        }

        public void SetThrottle(double value, int ticks = 20)
        {
            SendMessage("{\"rtrigger\":\"" + (int)value + "\",\"rt_ticks\":\"" + ticks + "\"}");
        }

        public void SetLeftRudder(double ticks)
        {
            SendMessage("{\"lbumper\":\"1\",\"lbumper_ticks\":\"" + (int)ticks + "\"}");
            SendMessage("{\"rbumper\":\"1\",\"rbumper_ticks\":\"0\"}");
        }

        public void SetRightRudder(double ticks)
        {
            SendMessage("{\"rbumper\":\"1\",\"rbumper_ticks\":\"" + (int)ticks + "\"}");
            SendMessage("{\"lbumper\":\"1\",\"lbumper_ticks\":\"0\"}");
        }

        public void ClearInputs()
        {
            SendMessage("{\"lbumper\":\"1\",\"lbumper_ticks\":\"0\"}");
            SendMessage("{\"lbumper\":\"1\",\"lbumper_ticks\":\"0\"}");
            SendMessage("{\"thumbly\":\"0\",\"thumbly_ticks\":\"1\"}");
            SendMessage("{\"thumblx\":\"0\",\"thumblx_ticks\":\"1\"}");
        }
    }
}
