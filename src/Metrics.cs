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

        // Note: These bounds are past the 'vehicile falls apart' threshold to make things easier.
        public static readonly PointF WorldSize = new PointF(6000, 6000);

        public static Uri Map_Zoom4_Full_20 => new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full_20.png"));
        public static Uri Map_Zoom4_Full => new Uri(System.IO.Path.GetFullPath("../../../../res/map_zoom4_full.png"));

        public static readonly int SCALE_Map4_20_TO_100 = 5;

        public static readonly double SCALE_METERS_TO_MAP4 = 0.31102730648;

        /* 
          How did we come to the meters to zoom factor?

          Measure the desert airport runway, longest part of the box zoomed in on onscreen map
          runway len = 1648.82px
          scale length = 92m
          scale = 206px
          thus:
          2.23913043478 px = 1m
          full scale runway = 736.366213593m

          zoom4: 229.03px
          zoom5: 458.74px
         */
    }
}