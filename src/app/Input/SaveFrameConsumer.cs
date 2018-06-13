using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;

namespace GTAPilot
{
    class SaveFrameConsumer
    {
        public FpsCounter FPS = new FpsCounter();

        public ConcurrentQueue<FrameData> _queue = new ConcurrentQueue<FrameData>();
        string _dir;

        public SaveFrameConsumer(string dir)
        {
            _dir = dir;

            Directory.CreateDirectory(dir);

            StartFlushThread();
            StartFlushThread();
            StartFlushThread();
            StartFlushThread();
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
