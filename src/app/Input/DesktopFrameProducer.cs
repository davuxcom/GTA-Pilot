using System;
using System.Drawing;

namespace GTAPilot
{
    class DesktopFrameProducer : IFrameProducer
    {
        public event Action<Bitmap> FrameProduced;

        private bool _isRunning;

        public void Begin()
        {
            _isRunning = true;
            var t = new System.Threading.Thread(() =>
            {
                // CONFIG
                var desktop = new DesktopDuplication.DesktopDuplicator(0, 2);
                while (_isRunning)
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

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
