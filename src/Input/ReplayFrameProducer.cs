using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace GTAPilot
{
    class ReplayFrameProducer : IFrameProducer
    {
        public event Action<int, Bitmap> FrameProduced;

        string[] _frames;
        int _currentId = 0;

        public ReplayFrameProducer(string dir)
        {
            _frames = Directory.GetFiles(dir);
        }

        public void Begin()
        {
            var t = new Thread(() =>
            {
                while (true)
                {
                    FrameProduced(_currentId, new Bitmap(_frames[_currentId++]));

                    if (_currentId >= _frames.Length) _currentId = 0;
                }
            });
            t.IsBackground = true;
            t.Priority = ThreadPriority.Highest;
            t.Start();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
