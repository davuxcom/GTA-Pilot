using GTAPilot.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using static GTAPilot.Interop.DwmApi;

namespace GTAPilot
{
    public partial class ImmersiveWindow : Window
    {
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public ImmersiveWindow()
        {
            InitializeComponent();

            Activated += Window_Activated;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Activated -= Window_Activated;

            var xboxAppHwnd = FindWindow("ApplicationFrameWindow", "Xbox");
            var handle = DwmApi.DwmRegisterThumbnail(new WindowInteropHelper(this).Handle, xboxAppHwnd);

            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags =
                    DWM_THUMBNAIL_PROPERTIES.DWM_TNP_VISIBLE +
                    DWM_THUMBNAIL_PROPERTIES.DWM_TNP_RECTSOURCE +
                    DWM_THUMBNAIL_PROPERTIES.DWM_TNP_RECTDESTINATION,
                fVisible = true
            };

            var height = (int)LayoutRoot.ActualHeight;
            props.rcSource = new RECT(0, 1200 - height, 1920, height);
            props.rcDestination = new RECT(0, 0, 1920, height);

            DwmUpdateThumbnailProperties(handle, props);
            DwmQueryThumbnailSourceSize(handle, out var sz);

            Trace.WriteLine($"DwmRegisterThumbnail: {handle} {sz}");
        }
    }
}
