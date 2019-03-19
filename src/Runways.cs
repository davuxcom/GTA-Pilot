using System.Drawing;

namespace GTAPilot
{
    class Runways
    {
        private static double LSI_Elevation = 50;

        public static Runway LSI_RW03 = new Runway(
            new PointF(2029, 4575), new PointF(2133, 4396), LSI_Elevation);
        public static Runway LSI_RW30L = new Runway(
            new PointF(2200, 4742), new PointF(2003, 4626), LSI_Elevation);
    }
}
