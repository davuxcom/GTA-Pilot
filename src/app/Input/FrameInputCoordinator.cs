using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace GTAPilot
{
    class FrameInputCoordinator
    {
        public FpsCounter EnqueuePerf = new FpsCounter();
        public FpsCounter DequeuePerf = new FpsCounter();

        IFrameProducer _producer;
        Action<FrameData> _consumer;
        ConcurrentQueue<FrameData> _input = new ConcurrentQueue<FrameData>();

        public FrameInputCoordinator(IFrameProducer producer, Action<FrameData> consumer)
        {
            _producer = producer;
            _consumer = consumer;
            _producer.FrameProduced += FrameProducer_FrameProduced;
        }

        public void Begin()
        {
            _producer.Begin();

          //  StartWorkerThread();
        }

        private void StartWorkerThread()
        {
            new Thread(() =>
            {
                while (true)
                {
                    if (_input.TryDequeue(out var nextFrame))
                    {
                        DequeuePerf.GotFrame();

                        _consumer.Invoke(nextFrame);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }).Start();
        }

        private void FrameProducer_FrameProduced(int frameId, System.Drawing.Bitmap frame)
        {
            EnqueuePerf.GotFrame();
            _consumer.Invoke(new FrameData(frameId, frame, Timeline.Duration.Elapsed.TotalSeconds));
           // _input.Enqueue(new FrameData(frameId, frame, Timeline.Duration.Elapsed.TotalSeconds));
        }
    }
}
