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
        private static double MIN_AXIS_VALUE = -12000;
        private static double MAX_AXIS_VALUE = 12000;

        public static PIDConfig Roll = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 6,
                I = 0.1,
                D = 0
            },
            PV = new PID.Range { Min = -45, Max = 45},
            OV = new PID.Range { Min = MIN_AXIS_VALUE, Max = MAX_AXIS_VALUE },
        };

        public static PIDConfig Pitch = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 5,
                I = 0.2,
                D = 0
            },
            PV = new PID.Range { Min = -90, Max = 90 },
            OV = new PID.Range { Min = MIN_AXIS_VALUE, Max = MAX_AXIS_VALUE },
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
