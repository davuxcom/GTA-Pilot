using System;
using System.Runtime.InteropServices;

namespace GTAPilot.Interop
{
    class User32
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr FindWindow(
            string lpClassName, 
            string lpWindowName);

        [DllImport("user32.dll", PreserveSig = true)]
        internal static extern bool GetWindowRect(
            IntPtr hwnd,
            out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetCursorPos(int x, int y);
    }
}
