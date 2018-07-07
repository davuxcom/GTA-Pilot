using Emgu.CV;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace GTAPilot
{
    class MenuReader
    {
        public bool IsInMenu { get; private set; }
        public bool IsInMap { get; private set; }
        public string SelectedMenuItem { get; private set; }
        public string SelectedGameMenuItem { get; private set; }
        public string SelectedSavePointItem { get; private set; }
        public PointF Location { get; private set; }

        static Rectangle LoadingTextRect = new Rectangle(1748, 1083, 86, 25);
        static string BackText = "SELECT";
        static string WaypointText = "WAYPOINT";

        static Rectangle TopMenuRect = new Rectangle(310, 240, 1302, 39);
        static int TopMenuItemVisibleCount = 6;
        static Rectangle Game_LeftMenuBar = new Rectangle(310, 298, 432, 200);
        static int Game_LeftMenuBarCount = 5;
        static Rectangle Game_SavePointList = new Rectangle(745, 339, 868, 606);
        static int Game_SavePointListVisibleCount = 15;

        public void HandleFrameArrived(Image<Bgr, byte> frame, DebugState debugState)
        {
            var text = Utils.ReadTextFromImage(frame.Copy(LoadingTextRect), debugState);
            IsInMenu = text == BackText || text == WaypointText;
            IsInMap = text == WaypointText;

            if (IsInMap)
            {
                Location = ReadMapLocation(frame, debugState);
               // Trace.WriteLine($"Location: {Location}");
            }
            else
            {
                Location = default(PointF);
            }

            SelectedMenuItem = FindSelectedItemInHorizontalRect(frame.Copy(TopMenuRect), TopMenuItemVisibleCount, debugState);
            SelectedGameMenuItem = FindSelectedItemInVerticalRect(frame.Copy(Game_LeftMenuBar), Game_LeftMenuBarCount, debugState);
            SelectedSavePointItem = FindSelectedItemInVerticalRect(frame.Copy(Game_SavePointList), Game_SavePointListVisibleCount, debugState);
        }

        private string FindSelectedItemInHorizontalRect(Image<Bgr, byte> colorImg, int count, DebugState debugState)
        {
            var img = colorImg.Convert<Hsv, byte>().InRange(new Hsv(0,0,120), new Hsv(180, 255, 255));
            var ret = new List<Tuple<int, double, Image<Gray,byte>>>();
            for (var i = 0; i < count; i++)
            {
                var buttonImg = img.Copy(new Rectangle(
                    (img.Width / count) * i,
                    0,
                    img.Width / count,
                    img.Height));
                ret.Add(new Tuple<int, double, Image<Gray,byte>>(i, buttonImg.CountNonzeroAsPercentage(), buttonImg));
            }
            return Utils.ReadTextFromImage(ret.OrderByDescending(i => i.Item2).First().Item3.Not(), debugState);
        }

        private string FindSelectedItemInVerticalRect(Image<Bgr, byte> colorImg, int count, DebugState debugState)
        {
            var img = colorImg.Convert<Hsv, byte>().InRange(new Hsv(0, 0, 120), new Hsv(180, 255, 255));
            var ret = new List<Tuple<int, double, Image<Gray, byte>>>();
            for (var i = 0; i < count; i++)
            {
                var buttonImg = img.Copy(new Rectangle(
                    0,
                    (img.Height / count) * i,
                    img.Width,
                    img.Height / count));
                ret.Add(new Tuple<int, double, Image<Gray, byte>>(i, buttonImg.CountNonzeroAsPercentage(), buttonImg));
            }
            var selectedButton = ret.OrderByDescending(i => i.Item2).First().Item3.Not();
            selectedButton = Utils.RemoveBlobs(selectedButton, 1, 10);

            return Utils.ReadTextFromImage(selectedButton, debugState);
        }

        public PointF ReadMapLocation(Image<Bgr,byte> img, DebugState debugState)
        {
            var hsv = img.Convert<Hsv, byte>();
            var onlyGreenBlobs = hsv.InRange(new Hsv(40, 40, 220), new Hsv(180, 100, 255));
            var blobs = Utils.DetectAndFilterBlobs(onlyGreenBlobs, 170, 220);
            if (blobs.Any())
            {
              //  Trace.WriteLine("Blobs; " + string.Join(" ", blobs.Select(b => b.Area).ToArray()));

                // Scale on map: 1007m = 207px
                // Scale factor: 
                var SCALE_MAP_PX_TO_METERS = 4.86473429952;
                var scale = SCALE_MAP_PX_TO_METERS * Metrics.SCALE_METERS_TO_MAP4;

                var franlinHangarBlob = blobs.OrderByDescending(b => b.Centroid.Y).First().Centroid;

                var center_pt = new Point((img.Width / 2), (img.Height / 2));
                var x_d = ((center_pt.X - franlinHangarBlob.X) * scale);
                var y_d = ((center_pt.Y - franlinHangarBlob.Y) * scale);

                var refererence_point = new PointF(2152, 4777);
                return new PointF((float)(refererence_point.X + x_d), (float)(refererence_point.Y + y_d));
            }

            debugState.Add(img);

            return default(PointF);
        }
    }
}
