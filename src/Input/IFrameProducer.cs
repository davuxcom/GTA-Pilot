using System;
using System.Drawing;

namespace GTAPilot
{
    interface IFrameProducer
    {
        event Action<int, Bitmap> FrameProduced;

        void Begin();
        void Stop();
    }
}