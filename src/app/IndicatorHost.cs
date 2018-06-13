using Emgu.CV;
using Emgu.CV.Structure;
using GTAPilot.Indicators_v2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot
{
    public class IndicatorData
    {
        public Image<Bgr, byte> Frame;
        public DateTime Timestamp;
        public int Id;
    }

    class IndicatorHost
    {
        public Indicator Roll = new Indicator(new RollIndicator_v2());
        public Indicator Pitch = new Indicator(new PitchIndicator_v2());
        public Indicator Airspeed = new Indicator(new AirspeedIndicator_v2());
        public Indicator Altitude = new Indicator(new AltitudeIndicator_v2());
        public Indicator Compass = new Indicator(new YawIndicator_v2());

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
            Pitch.Tick(frame);
            Airspeed.Tick(frame);
            Altitude.Tick(frame);
          //  Compass.Tick(frame);
        }
    }
}
