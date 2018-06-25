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
    }
}
