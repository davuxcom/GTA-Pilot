using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

       // public static PIDValue Roll = new PIDValue { P = 1.55, I = 0.3, D = 0.4 };
       // public static PIDValue Pitch = new PIDValue { P = 0.6, I = 0.4, D = 0.4 };




        public static PIDValue Speed = new PIDValue { P = 50, I = 0, D = 0.5 };
        public static PIDValue Heading = new PIDValue { P = 1.5, I = 0, D = 0 };

        public static double MenuLeadDelay = 800;
        public static double MenuExitDelay_GPS = 55;

        public static double MenuExitDelay_Input = 1100;
        public static double MenuExitDelay_Output = 1200;
        public static double MenuExitViewDelay = 845;


        public static double RollScale = 1;
        public static double PitchScale = 1;

        public static double Pitch_DeadZone = 8000;
        public static double Pitch_Max = 14000;
        public static double Roll_DeadZone = 8000;
        public static double Roll_Max = 12000;

        public static double RollTrim = 800;
    }
}
