using Emgu.CV;
using Emgu.CV.Structure;
using GTAPilot.Indicators;
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

    // IndicatorHandler takes a fullscreen bitmap image and processes it using each of the indicators in turn.
    // Threads are created such that there is a pipeline of reading the indicator data.  This way there is
    // less overall latency in that we don't wait for each indicator, we do one at a time and thus the last
    // in the order has the highest latency, and the first has almost no latency.
    //
    // There is a high const in calling Image.Copy such that it seems to yield better performance using a
    // strategy like the above as opposed to forking the image and handing it out to multiple threads concurrencly.
    class IndicatorHandler
    {
        public Indicator Roll = new Indicator(new RollIndicator());
        public Indicator Pitch = new Indicator(new PitchIndicator());
        public Indicator Airspeed = new Indicator(new AirspeedIndicator());
        public Indicator Altitude = new Indicator(new AltitudeIndicator());
        public Indicator Compass = new Indicator(new YawIndicator());

        public MenuReader Menu = new MenuReader();
        public LoadingReader Loading = new LoadingReader();

        enum Stage
        {
            Tick1 = 1,
            Tick2 = 2,
            Tick3 = 3,
            Tick4 = 4,
            Tick5 = 5,

            MenuTick = 6
        }

        class Data
        {
            public IndicatorData IndicatorData;
            public Stage Stage;
        }

        ConcurrentQueue<Data> _workItems = new ConcurrentQueue<Data>();
        FlightDataComputer _computer;
        long queueIncrementId;

        public IndicatorHandler(FlightDataComputer computer)
        {
            _computer = computer;

            new Thread(() =>
            {
                StartWorkerThread();

                // This delay exists because there seems to be a race condition in initializing EmguCV
                // when multiple threads go at it we see AccessViolationExceptions, mostly out of Blob detection.
                Thread.Sleep(1000);

                StartWorkerThread();
                StartWorkerThread();
                StartWorkerThread();
                StartWorkerThread();
                StartWorkerThread();
                StartWorkerThread();
            }).Start();
        }

        private void StartWorkerThread()
        {
            var t = new Thread(() =>
            {
                long localId = 0;
                while (true)
                {
                    if (queueIncrementId != localId && _workItems.TryDequeue(out var nextFrame))
                    {
                        Tick(nextFrame.Stage, nextFrame.IndicatorData);
                        if (nextFrame.Stage < Stage.Tick5)
                        {
                            Enqueue(new Data { Stage = ++nextFrame.Stage, IndicatorData = nextFrame.IndicatorData });
                        }
                    }
                    else
                    {
                        localId = queueIncrementId;
                        Thread.Sleep(1);
                    }
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        internal void HandleFrameArrived(FrameData data)
        {
            var frame = new TimelineFrame
            {
                Seconds = data.Seconds,
                Id = Timeline.LatestFrameId,
            };
            Timeline.Data[Timeline.LatestFrameId] = frame;

            if (Timeline.IsInGame)
            {
                Timeline.LatestFrameId++;
            }

            if (!Timeline.IsInGame)
            {
                // Drop a lot of frames for non-game mode.
                if (data.FrameId % 10 != 0) return;
            }

            var indicatorData = new IndicatorData
            {
                Frame = new Image<Bgr, byte>(data.Frame),
                Id = frame.Id,
                Seconds = data.Seconds,
            };

            Enqueue(new Data { Stage = Timeline.IsInGame ? Stage.Tick1 : Stage.MenuTick, IndicatorData = indicatorData });
        }

        private void Enqueue(Data data)
        {
            _workItems.Enqueue(data);
            queueIncrementId++;
        }

        void Tick(Stage stage, IndicatorData data)
        {
            switch (stage)
            {
                case Stage.Tick1:
                    Timeline.Data[data.Id].Roll.Value = Roll.Tick(data);
                    _computer.OnRollDataSampled(data.Id);
                    Timeline.Data[data.Id].Roll.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;
                    break;
                case Stage.Tick2:
                    Timeline.Data[data.Id].Pitch.Value = Pitch.Tick(data);
                    _computer.OnPitchDataSampled(data.Id);
                    Timeline.Data[data.Id].Pitch.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;
                    break;
                case Stage.Tick3:
                    Timeline.Data[data.Id].Speed.Value = Airspeed.Tick(data);
                    _computer.OnSpeedDataSampled(data.Id);
                    Timeline.Data[data.Id].Speed.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;
                    break;
                case Stage.Tick4:
                    Timeline.Data[data.Id].Altitude.Value = Altitude.Tick(data);
                    _computer.OnAltidudeDataSampled(data.Id);
                    Timeline.Data[data.Id].Altitude.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;
                    break;
                case Stage.Tick5:
                    Timeline.Data[data.Id].Heading.Value = Compass.Tick(data);
                    _computer.OnCompassDataSampled(data.Id);
                    Timeline.Data[data.Id].Heading.SecondsWhenComputed = Timeline.Duration.Elapsed.TotalSeconds - Timeline.Data[data.Id].Seconds;
                    Timeline.Data[data.Id].IsDataComplete = true;
                    break;
                case Stage.MenuTick:

                    var menuDS = new DebugState();
                    Menu.HandleFrameArrived(data, new DebugState());
                    Loading.HandleFrameArrived(data, menuDS);
                    Roll.Image = menuDS.Get(10);
               
                    // TODO: do something with 
                    break;
                default: throw new NotImplementedException();
            }
        }
    }
}
