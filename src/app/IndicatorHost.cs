using Emgu.CV;
using Emgu.CV.Structure;
using GTAPilot.Indicators_v2;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace GTAPilot
{
    public class IndicatorData
    {
        public Image<Bgr, byte> Frame;
        public double Seconds;
        public int Id;
    }

    class IndicatorHost
    {
        public Indicator Roll = new Indicator(new RollIndicator_v2());
        public Indicator Pitch = new Indicator(new PitchIndicator_v2());
        public Indicator Airspeed = new Indicator(new AirspeedIndicator_v2());
        public Indicator Altitude = new Indicator(new AltitudeIndicator_v2());
        public Indicator Compass = new Indicator(new YawIndicator_v2());

        ConcurrentQueue<IndicatorData> _stage2 = new ConcurrentQueue<IndicatorData>();
        ConcurrentQueue<IndicatorData> _stage3 = new ConcurrentQueue<IndicatorData>();
        ConcurrentQueue<IndicatorData> _stage4 = new ConcurrentQueue<IndicatorData>();
        ConcurrentQueue<IndicatorData> _stage5 = new ConcurrentQueue<IndicatorData>();
        FlightDataComputer _computer;

        public IndicatorHost(FlightDataComputer computer)
        {
            _computer = computer;

            StartWorkerThread(_stage2, (d) =>
            {
                Tick2(d);
                _stage3.Enqueue(d);
            });

            StartWorkerThread(_stage3, (d) =>
            {
                Tick3(d);
                _stage4.Enqueue(d);
            });


            StartWorkerThread(_stage4, (d) =>
            {
                Tick4(d);
                _stage5.Enqueue(d);
            });

            StartWorkerThread(_stage5, (d) =>
            {
                Tick5(d);
            });
            StartWorkerThread(_stage5, (d) =>
            {
                Tick5(d);
            });
        }

        private void StartWorkerThread(ConcurrentQueue<IndicatorData> target, Action<IndicatorData> next)
        {
            new Thread(() =>
            {
                while (true)
                {
                    if (target.TryDequeue(out var nextFrame))
                    {
                        next.Invoke(nextFrame);
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
            Timeline.Data[data.FrameId] = new TimelineFrame
            {
                Seconds = data.Seconds,
                Id = data.FrameId,
            };

            Timeline.LastFrameId = data.FrameId;

            var frame = new IndicatorData
            {
                Frame = new Image<Bgr, byte>(data.Frame),
                Id = data.FrameId,
                Seconds = data.Seconds,
            };

            Tick1(frame);
            _stage2.Enqueue(frame);
        }

        void Tick1(IndicatorData data)
        {
            Timeline.Data[data.Id].Roll.Value = Roll.Tick(data);
            _computer.OnRollDataSampled(data.Id);
            Timeline.Data[data.Id].Roll.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;

        }

        void Tick2(IndicatorData data)
        {
            Timeline.Data[data.Id].Pitch.Value = Pitch.Tick(data);
            _computer.OnPitchDataSampled(data.Id);
            Timeline.Data[data.Id].Pitch.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;

        }

        void Tick3(IndicatorData data)
        {
            Timeline.Data[data.Id].Speed.Value = Airspeed.Tick(data);
            _computer.OnSpeedDataSampled(data.Id);
            Timeline.Data[data.Id].Speed.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;

        }

        void Tick4(IndicatorData data)
        {
            Timeline.Data[data.Id].Altitude.Value = Altitude.Tick(data);
            _computer.OnAltidudeDataSampled(data.Id);
            Timeline.Data[data.Id].Altitude.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;

        }

        void Tick5(IndicatorData data)
        {
            Timeline.Data[data.Id].Heading.Value = Compass.Tick(data);

            _computer.OnCompassDataSampled(data.Id);
            Timeline.Data[data.Id].Heading.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;

            Timeline.Data[data.Id].IsComplete = true;
        }
    }
}
