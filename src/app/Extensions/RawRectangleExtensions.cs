using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
