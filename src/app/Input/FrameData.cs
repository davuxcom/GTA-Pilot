using System;
using System.Drawing;

namespace GTAPilot
{
    class FrameData
    {
        public Bitmap Frame { get; set; }
        public int FrameId { get; set; }
        public DateTime Time { get; set; }

        public FrameData(int frameId, Bitmap frame)
        {
            Frame = frame;
            FrameId = frameId;
            Time = DateTime.Now;
        }
    }
}
