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

    // IndicatorHandler takes a fullscreen bitmap image and 'ticks' it to each of the indicators in turn.
    // Threads are created such that there is a pipeline of reading the indicator data.  This way there is
    // less overall latency in that we don't wait for each indicator, we do one at a time and thus the last
    // in the order has the highest latency, and the first has almost no latency.
    //
    // There is a high const in calling Image.Copy such that it seems to yield better performance using a
    // strategy like the above as opposed to forking the image and handing it out to multiple threads concurrencly.
    class IndicatorHandler
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

        public IndicatorHandler(FlightDataComputer computer)
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
            var doneTime = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;
            Timeline.Data[data.Id].Heading.Value = Compass.Tick(data);

            _computer.OnCompassDataSampled(data.Id);
            Timeline.Data[data.Id].Heading.SecondsWhenComputed = doneTime;

            var prev = Timeline.LatestFrame(d => d.Heading.Value, data.Id);
            if (prev != null && !double.IsNaN(Timeline.Data[data.Id].Heading.Value))
            {
                var dT = doneTime - prev.Heading.SecondsWhenComputed;
                if (dT < 1)
                {
                    var dX = Math2.DiffAngles(Timeline.Data[data.Id].Heading.Value, prev.Heading.Value);
                    if (Math.Abs(dX) > 10)
                    {
                        // can't move more than 20 deg in one second
                        Timeline.Data[data.Id].Heading.Value = double.NaN;
                    }
                }
            }

            Timeline.Data[data.Id].IsComplete = true;
        }
    }
}
