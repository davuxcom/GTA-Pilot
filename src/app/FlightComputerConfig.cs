using System;

namespace GTAPilot
{
    class PIDConfig
    {
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
                P = 1.1,
                I = 0.05,
                D = 0
            },
            PV = new PID.Range { Min = -45, Max = 45},
            OV = new PID.Range { Min = short.MinValue, Max = short.MaxValue },
        };

        public static PIDConfig Pitch = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 1.4,
                I = 0.4,
                D = 0
            },
            PV = new PID.Range { Min = -90, Max = 90 },
            OV = new PID.Range { Min = short.MinValue, Max = short.MaxValue },
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
        };
    }
}
