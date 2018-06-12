using System;
using System.Collections.Concurrent;
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

        private void FrameProducer_FrameProduced(System.Drawing.Bitmap frame)
        {
            EnqueuePerf.GotFrame();

            _input.Enqueue(new FrameData(frame));
        }
    }
}
