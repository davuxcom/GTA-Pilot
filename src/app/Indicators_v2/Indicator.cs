namespace GTAPilot.Indicators_v2
{
    class Indicator
    {
        public FpsCounter Counter { get; }
        public double Value { get; private set; }
        public dynamic Image { get; private set; }

        ISimpleIndicator _indicator;
        IndicatorData _nextData;

        public Indicator(ISimpleIndicator indicator)
        {
            Counter = new FpsCounter();
            _indicator = indicator;
        }

        public void Tick(IndicatorData data)
        {
            object[] debugState = null;
            Value = _indicator.ReadValue(data.Frame, ref debugState);
            Image = (debugState != null ? debugState[0] : null);
            if (!double.IsNaN(Value))
            {
                Counter.GotFrame();
            }
        }
    }
}
