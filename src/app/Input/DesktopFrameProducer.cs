using System;
using System.Drawing;

namespace GTAPilot
{
    class DesktopFrameProducer : IFrameProducer
    {
        public event Action<int, Bitmap> FrameProduced;

        private bool _isRunning;
        private int _screenId;

        public DesktopFrameProducer(int screenId)
        {
            _screenId = screenId;
        }

        public void Begin()
        {
            _isRunning = true;
            var t = new System.Threading.Thread(() =>
            {
                int frameId = 0;
                var desktop = new DesktopDuplication.DesktopDuplicator(0, _screenId);
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
            t.Priority = System.Threading.ThreadPriority.Highest;
            t.Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
