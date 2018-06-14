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
        IFrameProducer _producer;
        FrameInputCoordinator _coordinator;
        public IndicatorHost IndicatorHost;
        FlightController _control = new FlightController();

        public SystemManager(IFrameProducer producer)
        {
            IndicatorHost = new IndicatorHost();
            _producer = producer;
            _coordinator = new FrameInputCoordinator(producer, FrameArrived);

            _coordinator.Begin();
            Timeline.Begin();

            _control.LockViewMin();
        }

        public SystemManager(IFrameProducer producer, Action<FrameData> consumer)
        {
            _producer = producer;
            _coordinator = new FrameInputCoordinator(producer, consumer);
            _coordinator.Begin();

            _control.LockViewMin();
        }

        internal FpsCounter GetCounter(FpsCounterType type)
        {
            switch(type)
            {
                case FpsCounterType.Capture_Enqueue: return _coordinator.EnqueuePerf;
                case FpsCounterType.Capture_Dequeue: return _coordinator.DequeuePerf;
                case FpsCounterType.Roll: return IndicatorHost.Roll.Counter;
                case FpsCounterType.Pitch: return IndicatorHost.Pitch.Counter;
                case FpsCounterType.Airspeed: return IndicatorHost.Airspeed.Counter;
                case FpsCounterType.Altitude: return IndicatorHost.Altitude.Counter;
                case FpsCounterType.Yaw: return IndicatorHost.Compass.Counter;
            }
            throw new NotImplementedException();
        }

        internal Bitmap GetLatestFrame(FpsCounterType type)
        {
            switch(type)
            {
                case FpsCounterType.Roll: return IndicatorHost.Roll.Image?.ToBitmap();
                case FpsCounterType.Pitch: return IndicatorHost.Pitch.Image?.ToBitmap();
                case FpsCounterType.Airspeed: return IndicatorHost.Airspeed.Image?.ToBitmap();
                case FpsCounterType.Altitude: return IndicatorHost.Altitude.Image?.ToBitmap();
                case FpsCounterType.Yaw: return IndicatorHost.Compass.Image?.ToBitmap();
            }
            throw new NotImplementedException();
        }

        private void FrameArrived(FrameData data)
        {
            IndicatorHost.HandleFrameArrived(data);
        }

        internal void StopCapture()
        {
            _producer.Stop();
        }
    }
}
