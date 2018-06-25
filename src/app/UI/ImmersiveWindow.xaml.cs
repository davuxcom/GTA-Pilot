using GTAPilot.Interop;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using static GTAPilot.Interop.DwmApi;

namespace GTAPilot
{
    public partial class ImmersiveWindow : Window
    {
        IntPtr _thumbnailId;
        ImmersiveOverlayWindow _overlay;

        public ImmersiveWindow()
        {
            InitializeComponent();

            new SystemManager(@"C:\workspace\FlightPlan.txt");

            _overlay = new ImmersiveOverlayWindow();

            Activated += Window_Activated;
        }

        private void ConnectWindowToXboxApp()
        {
            if (_thumbnailId != IntPtr.Zero)
            {
                DwmApi.DwmUnregisterThumbnail(_thumbnailId);
                _thumbnailId = IntPtr.Zero;
            }

            var xboxAppHwnd = User32.FindWindow("ApplicationFrameWindow", "Xbox");
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

        private void Window_Activated(object sender, EventArgs e)
        {
            Activated -= Window_Activated;

            ConnectWindowToXboxApp();

            var clientAreaScreenCoordinates = LayoutRoot.PointToScreen(new Point(0, 0));

            _overlay.Top = clientAreaScreenCoordinates.Y;
            _overlay.Left = clientAreaScreenCoordinates.X;
            _overlay.Height = LayoutRoot.ActualHeight;
            _overlay.Width = LayoutRoot.ActualWidth;
            _overlay.Show();
            _overlay.Owner = this;
        }
    }
}
