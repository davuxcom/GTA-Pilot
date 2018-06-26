using GTAPilot.Interop;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GTAPilot.Extensions
{
    public static class BitmapExtensions
    {
        public static ImageSource ToImageSource(this Bitmap bmp)
        {
            if (bmp == null) return null;

            var hBitmap = bmp.GetHbitmap();

            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                Gdi32.DeleteObject(hBitmap);
            }
        }
    }
}
