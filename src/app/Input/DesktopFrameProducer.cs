using System;
using System.Drawing;

namespace GTAPilot
{
    class DesktopFrameProducer : IFrameProducer
    {
        public event Action<Bitmap> FrameProduced;

        public void Begin()
        {
            var t = new System.Threading.Thread(() =>
            {
                // CONFIG
                var desktop = new DesktopDuplication.DesktopDuplicator(0, 2);
                while (true)
                {
                    var f = desktop.GetLatestFrame();
                    if (f == null)
                    {
                        System.Threading.Thread.Sleep(1);
                        continue;
                    }

                    FrameProduced(f.DesktopImage);
                }
            });
            t.Priority = System.Threading.ThreadPriority.Highest;
            t.Start();
        }
    }
}
