using System;
using System.Drawing;

namespace GTAPilot
{
    class DesktopFrameProducer : IFrameProducer
    {
        public event Action<int, Bitmap> FrameProduced;

        private bool _isRunning;
        private IntPtr _window;

        public DesktopFrameProducer(IntPtr window)
        {
            _window = window;
        }

        public void Begin()
        {
            _isRunning = true;
            var t = new System.Threading.Thread(() =>
            {
                int frameId = 0;
                var desktop = new DesktopDuplication.DesktopDuplicator(_window);
                while (_isRunning)
                {
                    var frame = desktop.GetLatestFrame();
                    if (frame == null)
                    {
                        System.Threading.Thread.Sleep(1);
                        continue;
                    }

                    FrameProduced(frameId++, frame);
                }
            });
            t.IsBackground = true;
            t.Priority = System.Threading.ThreadPriority.Highest;
            t.Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
