using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot.Indicators_v2
{
    class Indicator
    {
        public FpsCounter Counter { get; }
        public double Value { get; private set; }
        public dynamic Image { get; private set; }

        ISimpleIndicator _indicator;

        public Indicator(ISimpleIndicator indicator)
        {
            Counter = new FpsCounter();
            _indicator = indicator;
        }

        public void Tick(Image<Bgr, byte> frame)
        {
            object[] debugState = null;
            Value = _indicator.ReadValue(frame, ref debugState);
            Image = (debugState != null ? debugState[0] : null);

            Counter.GotFrame();
        }
    }
}
