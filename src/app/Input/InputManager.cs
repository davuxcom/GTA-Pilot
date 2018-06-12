using System;
using System.Threading;

namespace GTAPilot
{
    enum FpsCounterType
    {
        Capture_Enqueue,
        Capture_Dequeue,
    }


    class InputManager : IFrameConsumer
    {
        FrameInputCoordinator _coordinator;

        public InputManager()
        {
            _coordinator = new FrameInputCoordinator(new DesktopFrameProducer(), this);

            _coordinator.Begin();
        }

        internal int GetFpsCounter(FpsCounterType type)
        {
            switch(type)
            {
                case FpsCounterType.Capture_Enqueue: return _coordinator.EnqueuePerf.Fps;
                case FpsCounterType.Capture_Dequeue: return _coordinator.DequeuePerf.Fps;
            }
            throw new NotImplementedException();
        }

        public void FrameArrived(FrameData data)
        {
            // TODO do something with the frames
        }
    }
}
