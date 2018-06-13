using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot
{
    class FrameData
    {
        public Bitmap Frame { get; set; }
        public int FrameId { get; set; }
        public DateTime Time { get; set; }

        private static int s_frameId;

        public FrameData(Bitmap frame)
        {
            Frame = frame;
            FrameId = s_frameId++;
            Time = DateTime.Now;
        }
    }
}
