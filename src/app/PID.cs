using System;
using System.Diagnostics;
using System.Threading;

namespace GTAPilot
{
    public class PIDValue
    {
        public double P;
        public double I;
        public double D;
    }

    public static class TUNE
    {
        public static PIDValue Roll = new PIDValue { P = 2, I = 0, D = 0 };
        public static PIDValue Pitch = new PIDValue { P = 1, I = 0, D = 0 };
        public static PIDValue Speed = new PIDValue { P = 50, I = 0, D = 0.5 };
        public static PIDValue Heading = new PIDValue { P = 1.5, I = 0, D = 0 };

        public static double RollScale = 1;
        public static double PitchScale = 1;

        public static double Pitch_DeadZone = 8000;
        public static double Pitch_Max = 14000;
        public static double Roll_DeadZone = 8000;
        public static double Roll_Max = 12000;

        public static double RollTrim = 800;
    }

    public delegate double GetDouble();
    public delegate void SetDouble(double value);

    public class PID
    {
        //Gains
        public double kp;
        public double ki;
        public double kd;

        //Running Values
        private DateTime lastUpdate;
        public double lastPV;
        public double lastErr;
        public double errSum;

        //Reading/Writing Values
        private GetDouble readPV;
        private GetDouble readSP;
        private SetDouble writeOV;

        //Max/Min Calculation
        private double pvMax;
        private double pvMin;
        private double outMax;
        private double outMin;

        //Threading and Timing
        private double computeHz = 1.0f;
        private Thread runThread;

        public double PGain
        {
            get { return kp; }
            set { kp = value; }
        }

        public double IGain
        {
            get { return ki; }
            set { ki = value; }
        }

        public double DGain
        {
            get { return kd; }
            set { kd = value; }
        }

        public double PVMin
        {
            get { return pvMin; }
            set { pvMin = value; }
        }

        public double PVMax
        {
            get { return pvMax; }
            set { pvMax = value; }
        }

        public double OutMin
        {
            get { return outMin; }
            set { outMin = value; }
        }

        public double OutMax
        {
            get { return outMax; }
            set { outMax = value; }
        }

        public bool PIDOK
        {
            get { return runThread != null; }
        }

        public PID(double pG, double iG, double dG,
            double pMax, double pMin, double oMax, double oMin,
            GetDouble pvFunc, GetDouble spFunc, SetDouble outFunc)
        {
            kp = pG;
            ki = iG;
            kd = dG;
            pvMax = pMax;
            pvMin = pMin;
            outMax = oMax;
            outMin = oMin;
            readPV = pvFunc;
            readSP = spFunc;
            writeOV = outFunc;
        }

        ~PID()
        {
            readPV = null;
            readSP = null;
            writeOV = null;
        }

        public void Reset()
        {
            errSum = 0.0f;
            lastUpdate = DateTime.Now;
        }

        private double ScaleValue(double value, double valuemin, double valuemax, double scalemin, double scalemax)
        {
            double vPerc = (value - valuemin) / (valuemax - valuemin);
            double bigSpan = vPerc * (scalemax - scalemin);

            double retVal = scalemin + bigSpan;

            return retVal;
        }

        private double Clamp(double value, double min, double max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;
            return value;
        }

        public void Compute(bool log = false)
        {
            if (readPV == null || readSP == null || writeOV == null)
                return;

            double pv = readPV();
            double sp = readSP();

            //We need to scale the pv to +/- 100%, but first clamp it
            pv = Clamp(pv, pvMin, pvMax);
            pv = ScaleValue(pv, pvMin, pvMax, -1.0f, 1.0f);

            //We also need to scale the setpoint
            sp = Clamp(sp, pvMin, pvMax);
            sp = ScaleValue(sp, pvMin, pvMax, -1.0f, 1.0f);

            //Now the error is in percent...
            double err = sp - pv;

            double pTerm = err * kp;
            double iTerm = 0.0f;
            double dTerm = 0.0f;

            double partialSum = 0.0f;
            DateTime nowTime = DateTime.Now;

            if (lastUpdate != null)
            {
                double dT = (nowTime - lastUpdate).TotalSeconds;

                //Compute the integral if we have to...
                if (pv >= pvMin && pv <= pvMax)
                {
                    partialSum = errSum + dT * err;
                    iTerm = ki * partialSum;
                }

                if (dT != 0.0f) dTerm = kd * (err - lastErr) / dT;
                //dTerm = kd * (pv - lastPV) / dT;
            }

            lastUpdate = nowTime;
            errSum = partialSum;
            lastPV = pv;
            lastErr = err;

            //Now we have to scale the output value to match the requested scale
            double outReal = pTerm + iTerm + dTerm;

            outReal = Clamp(outReal, -1.0f, 1.0f);
            outReal = ScaleValue(outReal, -1.0f, 1.0f, outMin, outMax);

            //Write it out to the world
            writeOV(outReal);
        }
    }
}
