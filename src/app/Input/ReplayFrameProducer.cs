using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace GTAPilot
{
    class ReplayFrameProducer : IFrameProducer
    {
        string[] _frames;
        int[] _frameSet;
        int _currentId = 0;

        public ReplayFrameProducer(string dir, string framesetTxt)
        {
            _frames = Directory.GetFiles(dir);

            if (!string.IsNullOrWhiteSpace(framesetTxt))
            {
                _frameSet = File.ReadAllLines(framesetTxt).Select(l => int.Parse(l)).ToArray();
            }
        }

        public event Action<Bitmap> FrameProduced;

        public void Begin()
        {
            var t = new Thread(() =>
            {
                while (true)
                {
                    if (_frameSet != null)
                    {
                        FrameProduced(new Bitmap(_frames[_frameSet[_currentId++]]));

                        if (_currentId >= _frameSet.Length) _currentId = 0;

                    }
                    else
                    {
                        FrameProduced(new Bitmap(_frames[_currentId++]));

                        if (_currentId >= _frames.Length) _currentId = 0;
                    }
                }
            });
            t.Priority = ThreadPriority.Highest;
            t.Start();
        }
    }
}
