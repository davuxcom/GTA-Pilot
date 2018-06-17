using System;

namespace GTAPilot
{
    class PIDConfig
    {
        public double PV_Skew;
        public PID.Range PV;
        public PID.Range OV;

        public PID.Gain Gain;
    }

    class FlightComputerConfig
    {
        public static PIDConfig Roll = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 2,
                I = 0,
                D = 0
            },
            PV = new PID.Range { Min = 0, Max = 200},
            OV = new PID.Range { Min = 0, Max = UInt16.MaxValue - 1 },
            PV_Skew = 100,
        };

        public static PIDConfig Pitch = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 1,
                I = 0,
                D = 0
            },
            PV = new PID.Range { Min = 0, Max = 100 },
            OV = new PID.Range { Min = 0, Max = UInt16.MaxValue - 1 },
            PV_Skew = 50,
        };
        public static PIDConfig Speed = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 50,
                I = 0,
                D = 0.5
            },
            PV = new PID.Range { Min = 0, Max = 175 },
            OV = new PID.Range { Min = 0, Max = 255 },
            PV_Skew = 0,
        };
        public static PIDConfig Yaw = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 1.5,
                I = 0,
                D = 0
            },
            PV = new PID.Range { Min = 0, Max = 360 * 2 },
            OV = new PID.Range { Min = 0, Max = 100 },
        };
    }
}
