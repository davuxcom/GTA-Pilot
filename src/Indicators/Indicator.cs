using System.Collections.Generic;

namespace GTAPilot.Indicators
{
    class Indicator
    {
        public FpsCounter Counter { get; }
        public double Value { get; private set; }
        public double LastGoodValue => _indicator.LastGoodValue;
        public dynamic Image { get; private set; }
        public double CachedTuningValue => _indicator.CachedTuningValue;

        ISimpleIndicator _indicator;
        public HashSet<int> BadFrames = new HashSet<int>();

        public Indicator(ISimpleIndicator indicator)
        {
            Counter = new FpsCounter();
            _indicator = indicator;
            Image = new object[] { null, null, null, null };
        }

        public double Tick(IndicatorData data)
        {
            object[] debugState = new object[] { null, null, null, null };
            Value = _indicator.ReadValue(data, ref debugState);
            Image = debugState;

            if (double.IsNaN(Value))
            {
                BadFrames.Add(data.Id);
            }
            else
            {
                Counter.GotFrame();
            }
            return Value;
        }
    }
}