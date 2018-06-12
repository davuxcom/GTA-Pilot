using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot
{
    class IndicatorHost
    {
        public RollIndicator Roll = new RollIndicator();
        public PitchIndicator Pitch = new PitchIndicator();
        public SpeedIndicator Airspeed = new SpeedIndicator();
        public AltitudeIndicator Altitude = new AltitudeIndicator();
        public CompassIndicator Compass = new CompassIndicator();

        internal void HandleFrameArrived(FrameData data)
        {
            Timeline.Data[data.FrameId] = new TimelineFrame
            {
                Time = data.Time,
                Id = data.FrameId,
            };

            var frame = new IndicatorData
            {
                Frame = new Image<Bgr, byte>(data.Frame),
                Id = data.FrameId,
                Timestamp = data.Time,
            };

            Roll.Tick(frame);
          //  Pitch.Tick(frame);
          //  Airspeed.Tick(frame);
          //  Altitude.Tick(frame);
          //  Compass.Tick(frame);

        }
    }
}
