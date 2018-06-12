using System;
using System.Drawing;
using System.Threading;

namespace GTAPilot
{
    enum FpsCounterType
    {
        Capture_Enqueue,
        Capture_Dequeue,

        Roll,
        Pitch,
        Yaw,
        Altitude,
        Airspeed,

        // TODO
        Simulation,
        ControlOutput,
    }


    class SystemManager
    {


        FrameInputCoordinator _coordinator;
        IndicatorHost _indicatorHost;

        public SystemManager()
        {
            _indicatorHost = new IndicatorHost();
            _coordinator = new FrameInputCoordinator(new DesktopFrameProducer(), _indicatorHost.HandleFrameArrived);

            _coordinator.Begin();
        }

        internal FpsCounter GetCounter(FpsCounterType type)
        {
            switch(type)
            {
                case FpsCounterType.Capture_Enqueue: return _coordinator.EnqueuePerf;
                case FpsCounterType.Capture_Dequeue: return _coordinator.DequeuePerf;
                case FpsCounterType.Roll: return _indicatorHost.Roll.Counter;
                case FpsCounterType.Pitch: return _indicatorHost.Pitch.Counter;
                case FpsCounterType.Airspeed: return _indicatorHost.Airspeed.Counter;
                case FpsCounterType.Altitude: return _indicatorHost.Altitude.Counter;
                case FpsCounterType.Yaw: return _indicatorHost.Compass.Counter;
            }
            throw new NotImplementedException();
        }

        internal Bitmap GetLatestFrame(FpsCounterType type)
        {
            switch(type)
            {
                case FpsCounterType.Roll: return _indicatorHost.Roll.BestIntermediate?.ToBitmap();
                case FpsCounterType.Pitch: return _indicatorHost.Pitch.BestIntermediate?.ToBitmap();
                case FpsCounterType.Airspeed: return _indicatorHost.Airspeed.BestIntermediate?.ToBitmap();
                case FpsCounterType.Altitude: return _indicatorHost.Altitude.BestIntermediate?.ToBitmap();
                case FpsCounterType.Yaw: return _indicatorHost.Compass.BestIntermediate?.ToBitmap();
            }
            throw new NotImplementedException();
        }
    }
}
