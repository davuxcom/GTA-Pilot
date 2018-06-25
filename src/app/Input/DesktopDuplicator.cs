using GTAPilot.Extensions;
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
    public class DesktopDuplicationException : Exception
    {
        public DesktopDuplicationException(string message) : base(message){}
    }

    // Provides access to frame-by-frame updates of a particular desktop (i.e. one monitor).
    public class DesktopDuplicator
    {
        private Device mDevice;
        private Texture2DDescription mTextureDesc;
        private OutputDescription mOutputDesc;
        private OutputDuplication mDeskDupl;

        private Texture2D desktopImageTexture = null;
        private OutputDuplicateFrameInformation frameInfo = new OutputDuplicateFrameInformation();
        private int mWhichOutputDevice = -1;

        public DesktopDuplicator(int whichMonitor) : this(0, whichMonitor) { }

        public DesktopDuplicator(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            this.mWhichOutputDevice = whichOutputDevice;
            var adapter = new Factory1().GetAdapter1(whichGraphicsCardAdapter);
            this.mDevice = new Device(adapter);
            Output output = adapter.GetOutput(whichOutputDevice);

            var output1 = output.QueryInterface<Output1>();
            this.mOutputDesc = output.Description;
            this.mTextureDesc = new Texture2DDescription()
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = this.mOutputDesc.DesktopBounds.Width(),
                Height = this.mOutputDesc.DesktopBounds.Height(),
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            this.mDeskDupl = output1.DuplicateOutput(mDevice);
        }

        public Bitmap GetLatestFrame()
        {
            if (RetrieveFrame()) return null;
            try
            {
                return ProcessFrame();
            }
            finally
            {
                mDeskDupl.ReleaseFrame();
            }
        }

        private bool RetrieveFrame()
        {
            if (desktopImageTexture == null) desktopImageTexture = new Texture2D(mDevice, mTextureDesc);

            SharpDX.DXGI.Resource desktopResource = null;
            frameInfo = new OutputDuplicateFrameInformation();
            try
            {
                mDeskDupl.AcquireNextFrame(-1, out frameInfo, out desktopResource);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    return true;
                }
                else
                {
                    throw;
                }
            }
            using (var tempTexture = desktopResource.QueryInterface<Texture2D>())
                mDevice.ImmediateContext.CopyResource(tempTexture, desktopImageTexture);
            desktopResource.Dispose();
            return false;
        }
        
        private Bitmap ProcessFrame()
        {
            var mapSource = mDevice.ImmediateContext.MapSubresource(desktopImageTexture, 0, MapMode.Read, MapFlags.None);

            var newImage = new System.Drawing.Bitmap(mOutputDesc.DesktopBounds.Width(), mOutputDesc.DesktopBounds.Height(), PixelFormat.Format32bppRgb);
            var boundsRect = new System.Drawing.Rectangle(0, 0, mOutputDesc.DesktopBounds.Width(), mOutputDesc.DesktopBounds.Height());
            var mapDest = newImage.LockBits(boundsRect, ImageLockMode.WriteOnly, newImage.PixelFormat);
            var sourcePtr = mapSource.DataPointer;
            var destPtr = mapDest.Scan0;

            for (int y = 0; y < mOutputDesc.DesktopBounds.Height(); y++)
            {
                Utilities.CopyMemory(destPtr, sourcePtr, mOutputDesc.DesktopBounds.Width() * 4);

                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }

            newImage.UnlockBits(mapDest);
            mDevice.ImmediateContext.UnmapSubresource(desktopImageTexture, 0);
            return newImage;
        }
    }
}