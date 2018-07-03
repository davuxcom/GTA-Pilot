using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot
{
    class MenuReader
    {
        public bool IsInMenu { get; private set; }
        public bool IsInMap { get; private set; }
        public string SelectedGamePivot { get; private set; }

        static Rectangle LoadingTextRect = new Rectangle(1748, 1083, 86, 25);
        static string BackText = "Back";
        static string WaypointText = "Waypoint";

        static Rectangle TopMenuRect = new Rectangle(310, 240, 1302, 39);
        static int TopMenuItemVisibleCount = 6;
        static Rectangle Game_LeftMenuBar = new Rectangle(310, 298, 432, 200);
        static int Game_LeftMenuBarCount = 5;
        static Rectangle Game_SavePointList = new Rectangle(745, 339, 868, 606);
        static int Game_SavePointListVisibleCount = 15;

        public void HandleFrameArrived(IndicatorData data, DebugState debugState)
        {
            var text = Utils.ReadTextFromImage(data.Frame.Copy(LoadingTextRect), debugState);
            IsInMenu = text == BackText || text == WaypointText;
            IsInMap = text == WaypointText;

        }
    }
}
