using System;

namespace GTAPilot
{
    public class PID
    {
        public class Gain
        {
            public double P;
            public double I;
            public double D;
        }

        public class Range
        {
            public double Min;
            public double Max;

            public double Mid => Min + ((Max - Min) / 2);
        }

        public Gain Gains;
        public Range PV;
        public Range OV;

        // Running Values
        double lastPV;
        double lastErr;
        double errSum;

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

            double err = sp - pv; // Error in percent.

            double pTerm = err * Gains.P;
            double iTerm = 0.0f;
            double dTerm = 0.0f;

            double partialSum = 0.0f;

            if (dT > 0)
            {
                // Compute the integral if we have to...
                if (pv >= PV.Min && pv <= PV.Max)
                {
                    partialSum = errSum + dT * err;
                    iTerm = Gains.I * partialSum;
                }

                if (dT != 0.0f) dTerm = Gains.D * (err - lastErr) / dT;
                // Fixed from the initial version:
                //dTerm = kd * (pv - lastPV) / dT;
            }

            errSum = partialSum;
            lastPV = pv;
            lastErr = err;

            // Now we have to scale the output value to match the requested scale
            double outReal = pTerm + iTerm + dTerm;

            outReal = Clamp(outReal, -1.0f, 1.0f);
            outReal = ScaleValue(outReal, -1.0f, 1.0f, OV.Min, OV.Max);

            return outReal;
        }

        public void ClearError()
        {
            errSum = 0;
            lastErr = 0;
        }
    }
}
