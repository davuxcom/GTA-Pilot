using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace GTAPilot
{
    class ReplayFrameProducer : IFrameProducer
    {
        List<Bitmap> _frames = new List<Bitmap>();

        public ReplayFrameProducer(string dir)
        {
            foreach(var f in Directory.GetFiles(dir).Take(500))
            {
                _frames.Add(new Bitmap(f));
            }
        }

        public event Action<Bitmap> FrameProduced;

        public void Begin()
        {
            new Thread(() =>
            {
                int currentId = 0;
                while (true)
                {
                    FrameProduced(_frames[currentId++]);

                    if (currentId >= _frames.Count) currentId = 0;

                    Thread.Sleep(1000 / 60); // 60fps
                }
            }).Start();
        }
    }
}
