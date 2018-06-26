using GTAPilot.Extensions;
using GTAPilot.Interop;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace DesktopDuplication
{
    // Provides access to frame-by-frame updates of a particular desktop (i.e. one monitor).
    public class DesktopDuplicator
    {
        private Device _device;
        private Texture2DDescription _desktopTextureDescription;
        private OutputDescription _outputDescription;
        private OutputDuplication _desktopDuplication;
        private Texture2D _desktopImageTexture;

        public DesktopDuplicator(IntPtr window)
        {
            User32.GetWindowRect(window, out var rectNative);
            var windowRect = rectNative.ToRect();

            // Search all outputs on all adapters and find the window rect.
            var factory = new Factory1();
            for (var j = 0; j < factory.GetAdapterCount1(); j++)
            {
                var adapter = factory.GetAdapter1(j);

                for (var i = 0; i < adapter.GetOutputCount(); i++)
                {
                    var output = adapter.GetOutput(i);
                    var bounds = output.Description.DesktopBounds.ToRectangle();
                    if (bounds.Contains(windowRect))
                    {
                        Initialize(0, i);
                        return;
                    }
                }
            }

            throw new Exception($"Didn't find the {window} window on any display output.");
        }

        private DesktopDuplicator(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            Initialize(whichGraphicsCardAdapter, whichOutputDevice);
        }

        private void Initialize(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            var adapter = new Factory1().GetAdapter1(whichGraphicsCardAdapter);
            this._device = new Device(adapter);
            Output output = adapter.GetOutput(whichOutputDevice);

            var output1 = output.QueryInterface<Output1>();
            this._outputDescription = output.Description;
            this._desktopTextureDescription = new Texture2DDescription()
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = this._outputDescription.DesktopBounds.Width(),
                Height = this._outputDescription.DesktopBounds.Height(),
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            this._desktopImageTexture = new Texture2D(_device, _desktopTextureDescription);
            this._desktopDuplication = output1.DuplicateOutput(_device);
        }

        public Bitmap GetLatestFrame()
        {
            if (_desktopDuplication.TryAcquireNextFrame(-1, out var frameInfo, out var desktopResource) == SharpDX.DXGI.ResultCode.WaitTimeout.Result)
            {
                return null;
            }

            using (var tempTexture = desktopResource.QueryInterface<Texture2D>())
            {
                _device.ImmediateContext.CopyResource(tempTexture, _desktopImageTexture);
            }
            desktopResource.Dispose();

            try
            {
                return ProcessFrame();
            }
            finally
            {
                _desktopDuplication.ReleaseFrame();
            }
        }

        private Bitmap ProcessFrame()
        {
            var mapSource = _device.ImmediateContext.MapSubresource(_desktopImageTexture, 0, MapMode.Read, MapFlags.None);

            var newImage = new System.Drawing.Bitmap(_outputDescription.DesktopBounds.Width(), _outputDescription.DesktopBounds.Height(), PixelFormat.Format32bppRgb);
            var boundsRect = new System.Drawing.Rectangle(0, 0, _outputDescription.DesktopBounds.Width(), _outputDescription.DesktopBounds.Height());
            var mapDest = newImage.LockBits(boundsRect, ImageLockMode.WriteOnly, newImage.PixelFormat);
            var sourcePtr = mapSource.DataPointer;
            var destPtr = mapDest.Scan0;

            for (int y = 0; y < _outputDescription.DesktopBounds.Height(); y++)
            {
                Utilities.CopyMemory(destPtr, sourcePtr, _outputDescription.DesktopBounds.Width() * 4);

                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }

            newImage.UnlockBits(mapDest);
            _device.ImmediateContext.UnmapSubresource(_desktopImageTexture, 0);
            return newImage;
        }
    }
}