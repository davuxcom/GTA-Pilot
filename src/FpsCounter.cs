using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace GTAPilot
{
    // A tool to measure performacne without slowing down the target
    // Not threadsafe in terms of data (frames may be dropped), but should
    // be good enough for our purposes.
    public class FpsCounter
    {
        private static Stopwatch s_time = Stopwatch.StartNew();
        private const int MAX_FRAMES = 120;

        private double[] _frames = new double[MAX_FRAMES];
        private long _frameCounter = 0;

        public int Fps { get; set; }

        public void GotFrame()
        {
            var index = (int)(Interlocked.Increment(ref _frameCounter) % MAX_FRAMES);
            _frames[index] = s_time.Elapsed.TotalSeconds;

            Calculate(index);
        }

        private void Calculate(int startIndex)
        {
            var nowSeconds = s_time.Elapsed.TotalSeconds;

            var count = 0;
            for (var i = startIndex; i >= 0; i--)
            {
                if (nowSeconds - _frames[i] > 1) break;
                count++;
            }

            for (var i = MAX_FRAMES - 1; i > startIndex; i--)
            {
                if (nowSeconds - _frames[i] > 1) break;
                count++;
            }

            Fps = count;
        }
    }
}
