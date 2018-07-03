using Emgu.CV.Structure;
using System.Drawing;

namespace GTAPilot
{
    class LoadingReader
    {
        public bool IsLoading { get; private set; }
        public string LoadingTextRead { get; private set; }

        static Rectangle LoadingTextRect = new Rectangle(1613, 1085, 68, 19);
        static string LoadingText = "LOADING";

        public void HandleFrameArrived(IndicatorData data, DebugState debugState)
        {
            var text = Utils.ReadTextFromImage(data.Frame.Copy(LoadingTextRect), debugState);
            IsLoading = text == LoadingText;
            LoadingTextRead = text;
        }
    }
}
