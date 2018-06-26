using SharpDX.Mathematics.Interop;

namespace GTAPilot.Extensions
{
    public static class RawRectangleExtensions
    {
        public static int Width(this RawRectangle rect)
        {
            return rect.Right - rect.Left;
        }

        public static int Height(this RawRectangle rect)
        {
            return rect.Bottom - rect.Top;
        }

        public static System.Drawing.Rectangle ToRectangle(this RawRectangle rect)
        {
            return new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Width(), rect.Height());
        }
    }
}
