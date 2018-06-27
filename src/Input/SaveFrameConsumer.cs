using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace GTAPilot
{
    class SaveFrameConsumer
    {
        public FpsCounter FPS = new FpsCounter();
        public int BufferedFrames => _queue.Count;

        private ConcurrentQueue<FrameData> _queue = new ConcurrentQueue<FrameData>();
        private string _dir;

        public SaveFrameConsumer(string dir)
        {
            _dir = dir;

            Directory.CreateDirectory(dir);

            for (var i = 0; i < 4; i++) StartFlushThread();
        }

        private void StartFlushThread()
        {
            new Thread(() =>
            {
                while (true)
                {
                    if (_queue.TryDequeue(out var ret))
                    {
                        ret.Frame.Save($"{_dir}\\{((ret.FrameId).ToString().PadLeft(4, '0'))}.bmp");
                        FPS.GotFrame();
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }).Start();
        }

        internal void HandleFrameArrived(FrameData data)
        {
            _queue.Enqueue(data);
        }
    }
}
