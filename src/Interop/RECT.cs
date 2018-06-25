using System.Runtime.InteropServices;

namespace GTAPilot.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left, top, right, bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left; this.top = top;
            this.right = right; this.bottom = bottom;
        }

        public System.Drawing.Rectangle ToRect()
        {
            return new System.Drawing.Rectangle(left, top, right - left, bottom - top);
        }
    }
}
