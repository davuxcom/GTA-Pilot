using Emgu.CV.Structure;
using System.Drawing;

namespace GTAPilot
{
    class LoadingReader
    {
        public bool IsLoading { get; private set; }

        static Rectangle LoadingTextRect = new Rectangle(1613, 1085, 68, 19);
        static string LoadingText = "Loading";

        public void HandleFrameArrived(IndicatorData data, DebugState debugState)
        {
            var loadingTextFocus = data.Frame.Copy(LoadingTextRect);
            var blackImg = loadingTextFocus.Convert<Hsv, byte>()[2];
            var text = Utils.ReadTextFromImage(blackImg, debugState);
            IsLoading = text == LoadingText;

            debugState.Add(loadingTextFocus);
            debugState.Add(blackImg);
        }
    }
}
