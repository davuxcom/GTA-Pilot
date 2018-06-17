using System;

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

        private double ScaleValue(double value, double valuemin, double valuemax, double scalemin, double scalemax)
        {
            double vPerc = (value - valuemin) / (valuemax - valuemin);
            double bigSpan = vPerc * (scalemax - scalemin);
            return scalemin + bigSpan;
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
            integral = integral + (error * dT);
            var derivative = (error - error_prior) / dT;
            var output = Gains.P * error + Gains.I * integral + Gains.D * derivative;
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
