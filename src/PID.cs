namespace GTAPilot
{
    public class PID
    {
        public class Gain
        {
            public double P { get; set; }
            public double I { get; set; }
            public double D { get; set; }
        }

        public class Range
        {
            public double Min;
            public double Max;
        }

        public Gain Gains;
        public Range PV;
        public Range OV;

        double error_prior;
        double integral;

        private double ScaleValue(double value, double valueMin, double valueMax, double scaleMin, double scaleMax)
        {
            double vPerc = (value - valueMin) / (valueMax - valueMin);
            double bigSpan = vPerc * (scaleMax - scaleMin);
            return scaleMin + bigSpan;
        }

        private double Clamp(double value, double min, double max)
        {
            if (value > max) return max;
            if (value < min) return min;
            return value;
        }

        public double Compute(double pv, double sp, double dT)
        {
            pv = Clamp(pv, PV.Min, PV.Max);
            pv = ScaleValue(pv, PV.Min, PV.Max, -1.0f, 1.0f);

            sp = Clamp(sp, PV.Min, PV.Max);
            sp = ScaleValue(sp, PV.Min, PV.Max, -1.0f, 1.0f);

            var error = sp - pv;
            integral = integral + (Gains.I * error * dT);
            integral = Clamp(integral, -1.0f, 1.0f);
            var derivative = (error - error_prior) / dT;
            var output = Gains.P * error + integral - Gains.D * derivative;
            error_prior = error;

            output = Clamp(output, -1.0f, 1.0f);
            output = ScaleValue(output, -1.0f, 1.0f, OV.Min, OV.Max);
            return output;
        }

        public void ClearError()
        {
            integral = 0;
            error_prior = 0;
        }
    }
}
