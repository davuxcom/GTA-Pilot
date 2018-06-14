using System;
using System.Drawing;

namespace GTAPilot
{
    class FrameData
    {
        public Bitmap Frame { get; set; }
        public int FrameId { get; set; }
        public double Seconds { get; set; }

        public FrameData(int frameId, Bitmap frame, double seconds)
        {
            Frame = frame;
            FrameId = frameId;
            Seconds = seconds;
        }
    }
}
