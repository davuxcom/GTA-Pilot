using System;
using System.Drawing;
using System.IO;
using System.Threading;

namespace GTAPilot
{
    class ReplayFrameProducer : IFrameProducer
    {
        string[] _frames;
        int _currentId = 0;

        public ReplayFrameProducer(string dir)
        {
            _frames = Directory.GetFiles(dir);
        }

        public event Action<Bitmap> FrameProduced;

        public void Begin()
        {
            var t = new Thread(() =>
            {
                while (true)
                {
                    FrameProduced(new Bitmap(_frames[_currentId++]));

                    if (_currentId >= _frames.Length) _currentId = 0;

                    // Thread.Sleep(1000 / 60); // 60fps
                }
            });
            t.Priority = ThreadPriority.Highest;
            t.Start();
        }
    }
}
