using System;
using System.Drawing;

namespace GTAPilot
{
    class Metrics
    {
        // The size of the desktop is hard-coded here, since we'll need some offset changes to support
        // either 1920x1080 (letterboxing prevents any loss of detail) or larger displays which have extra pixels
        // relative to what the Xbox app is producing.
        public static readonly Rectangle Frame = new Rectangle(0, 0, 1920, 1200);

        public static readonly PointF WorldSize = new PointF(6000, 6000);

        public static Uri Map_Zoom4_Full_20 => new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full_20.png"));

        public static readonly int SCALE_Map_20_TO_100 = 5;
    }
}