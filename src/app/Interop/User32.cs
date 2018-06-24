using System;
using System.Runtime.InteropServices;

namespace GTAPilot.Interop
{
    class User32
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    }
}
