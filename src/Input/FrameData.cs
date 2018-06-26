using System.Drawing;

namespace GTAPilot
{
    class FrameData
    {
        public Bitmap Frame { get; }
        public int FrameId { get; }
        public double Seconds { get; }

        public FrameData(int frameId, Bitmap frame, double seconds)
        {
            Frame = frame;
            FrameId = frameId;
            Seconds = seconds;
        }
    }
}
