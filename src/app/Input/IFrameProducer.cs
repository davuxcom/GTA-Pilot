using System;
using System.Drawing;

namespace GTAPilot
{
    interface IFrameProducer
    {
        event Action<Bitmap> FrameProduced;

        void Begin();
        void Stop();
    }
}