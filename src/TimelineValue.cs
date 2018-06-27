namespace GTAPilot
{
    public class TimelineValue
    {
        // Sampled data value from the indicator image.
        public double Value = double.NaN;
        public double SecondsWhenComputed = double.NaN;
        // Controller output we are sending.
        public double OutputValue = double.NaN;
        // Controller input read from XInputGetState in XboxApp.exe (controller values from manually manipulating it)
        public double InputValue = double.NaN;
        // Desired state value at the time.
        public double SetpointValue = double.NaN;

        public object ForIndicatorUse;
    }
}
