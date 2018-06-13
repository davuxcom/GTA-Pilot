using System.Collections.Generic;

namespace GTAPilot.Indicators_v2
{
    class Indicator
    {
        public FpsCounter Counter { get; }
        public double Value { get; private set; }
        public dynamic Image { get; private set; }

        ISimpleIndicator _indicator;
        public HashSet<int> BadFrames = new HashSet<int>();

        public Indicator(ISimpleIndicator indicator)
        {
            Counter = new FpsCounter();
            _indicator = indicator;
            Image = new object[] { null, null, null, null };
        }

        public void Tick(IndicatorData data)
        {
            object[] debugState = new object[] { null, null, null, null };
            Value = _indicator.ReadValue(data.Frame, ref debugState);
            Image = debugState;
            if (!double.IsNaN(Value))
            {
                Counter.GotFrame();
            }
            else
            {
                BadFrames.Add(data.Id);
            }
        }
    }
}
