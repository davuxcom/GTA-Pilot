
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GTAPilot.Extensions
{
    public static class BitmapExtensions
    {
        class Gdi32
        {
            [DllImport("gdi32.dll", PreserveSig = true)]
            internal static extern bool DeleteObject(IntPtr objectHandle);
        }

        public static ImageSource ToImageSource(this Bitmap bmp)
        {
            ImageSource bitmapSource = null;

            if (bmp == null) return null;
           // try
            {
                var hBitmap = bmp.GetHbitmap();

                try
                {
                    bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
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
        //  catch(Exception ex)
            {
          //      Trace.WriteLine(ex);
            }
            return bitmapSource;
        }
    }
}
