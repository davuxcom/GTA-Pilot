using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace GTAPilot.Interop
{
    class DwmApi
    {
        [DllImport("dwmapi.dll", PreserveSig = false)]
        internal static extern IntPtr DwmRegisterThumbnail(
            IntPtr dest, 
            IntPtr source);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        internal static extern void DwmUnregisterThumbnail(IntPtr hThumbnail);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        internal static extern void DwmUpdateThumbnailProperties(
            IntPtr hThumbnail, 
            DWM_THUMBNAIL_PROPERTIES props);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        internal static extern void DwmQueryThumbnailSourceSize(
            IntPtr hThumbnail, 
            out Size size);

        [StructLayout(LayoutKind.Sequential)]
        internal class DWM_THUMBNAIL_PROPERTIES
        {
            public DWM_TNP dwFlags;
            public RECT rcDestination;
            public RECT rcSource;
            public byte opacity;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fVisible;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fSourceClientAreaOnly;
        }

        [Flags]
        internal enum DWM_TNP : uint
        {
            DWM_TNP_RECTDESTINATION = 0x00000001,
            DWM_TNP_RECTSOURCE = 0x00000002,
            DWM_TNP_OPACITY = 0x00000004,
            DWM_TNP_VISIBLE = 0x00000008,
            DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010,
        };
    }
}
