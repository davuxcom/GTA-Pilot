// TODO: Imported, this file needs cleanup

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Rectangle = SharpDX.Rectangle;

namespace DesktopDuplication
{
    public class DesktopDuplicationException : Exception
    {
        public DesktopDuplicationException(string message)
            : base(message)
        {
        }
    }
}



namespace DesktopDuplication
{
    /// <summary>
    /// Provides image data, cursor data, and image metadata about the retrieved desktop frame.
    /// </summary>
    public class DesktopFrame
    {
        /// <summary>
        /// Gets the bitmap representing the last retrieved desktop frame. This image spans the entire bounds of the specified monitor.
        /// </summary>
        public Bitmap DesktopImage { get; internal set; }

        /// <summary>
        /// Gets a list of the rectangles of pixels in the desktop image that the operating system moved to another location within the same image.
        /// </summary>
        /// <remarks>
        /// To produce a visually accurate copy of the desktop, an application must first process all moved regions before it processes updated regions.
        /// </remarks>
        public MovedRegion[] MovedRegions { get; internal set; }

        /// <summary>
        /// Returns the list of non-overlapping rectangles that indicate the areas of the desktop image that the operating system updated since the last retrieved frame.
        /// </summary>
        /// <remarks>
        /// To produce a visually accurate copy of the desktop, an application must first process all moved regions before it processes updated regions.
        /// </remarks>
        public System.Drawing.Rectangle[] UpdatedRegions { get; internal set; }

        /// <summary>
        /// The number of frames that the operating system accumulated in the desktop image surface since the last retrieved frame.
        /// </summary>
        public int AccumulatedFrames { get; internal set; }

        /// <summary>
        /// Gets the location of the top-left-hand corner of the cursor. This is not necessarily the same position as the cursor's hot spot, which is the location in the cursor that interacts with other elements on the screen.
        /// </summary>
        public System.Drawing.Point CursorLocation { get; internal set; }

        /// <summary>
        /// Gets whether the cursor on the last retrieved desktop image was visible.
        /// </summary>
        public bool CursorVisible { get; internal set; }

        /// <summary>
        /// Gets whether the desktop image contains protected content that was already blacked out in the desktop image.
        /// </summary>
        public bool ProtectedContentMaskedOut { get; internal set; }

        /// <summary>
        /// Gets whether the operating system accumulated updates by coalescing updated regions. If so, the updated regions might contain unmodified pixels.
        /// </summary>
        public bool RectanglesCoalesced { get; internal set; }
    }
}

namespace DesktopDuplication
{
    /// <summary>
    /// Describes the movement of an image rectangle within a desktop frame.
    /// </summary>
    /// <remarks>
    /// Move regions are always non-stretched regions so the source is always the same size as the destination.
    /// </remarks>
    public struct MovedRegion
    {
        /// <summary>
        /// Gets the location from where the operating system copied the image region.
        /// </summary>
        public System.Drawing.Point Source { get; internal set; }

        /// <summary>
        /// Gets the target region to where the operating system moved the image region.
        /// </summary>
        public System.Drawing.Rectangle Destination { get; internal set; }
    }
}



namespace DesktopDuplication
{
    /// <summary>
    /// Provides access to frame-by-frame updates of a particular desktop (i.e. one monitor), with image and cursor information.
    /// </summary>
    public class DesktopDuplicator
    {
        private Device mDevice;
        private Texture2DDescription mTextureDesc;
        private OutputDescription mOutputDesc;
        private OutputDuplication mDeskDupl;

        private Texture2D desktopImageTexture = null;
        private OutputDuplicateFrameInformation frameInfo = new OutputDuplicateFrameInformation();
        private int mWhichOutputDevice = -1;

        private Bitmap finalImage1, finalImage2;
        private bool isFinalImage1 = false;
        private Bitmap FinalImage
        {
            get
            {
                return isFinalImage1 ? finalImage1 : finalImage2;
            }
            set
            {
                if (isFinalImage1)
                {
                    finalImage2 = value;
                    //  if (finalImage1 != null) finalImage1.Dispose();
                }
                else
                {
                    finalImage1 = value;
                    // if (finalImage2 != null) finalImage2.Dispose();
                }
                isFinalImage1 = !isFinalImage1;
            }
        }

        /// <summary>
        /// Duplicates the output of the specified monitor.
        /// </summary>
        /// <param name="whichMonitor">The output device to duplicate (i.e. monitor). Begins with zero, which seems to correspond to the primary monitor.</param>
        public DesktopDuplicator(int whichMonitor)
            : this(0, whichMonitor)
        { }

        /// <summary>
        /// Duplicates the output of the specified monitor on the specified graphics adapter.
        /// </summary>
        /// <param name="whichGraphicsCardAdapter">The adapter which contains the desired outputs.</param>
        /// <param name="whichOutputDevice">The output device to duplicate (i.e. monitor). Begins with zero, which seems to correspond to the primary monitor.</param>
        public DesktopDuplicator(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            this.mWhichOutputDevice = whichOutputDevice;
            Adapter1 adapter = null;
            try
            {
                adapter = new Factory1().GetAdapter1(whichGraphicsCardAdapter);
            }
            catch (SharpDXException)
            {
                throw new DesktopDuplicationException("Could not find the specified graphics card adapter.");
            }
            this.mDevice = new Device(adapter);
            Output output = null;
            try
            {
                output = adapter.GetOutput(whichOutputDevice);
            }
            catch (SharpDXException)
            {
                throw new DesktopDuplicationException("Could not find the specified output device.");
            }
            var output1 = output.QueryInterface<Output1>();
            this.mOutputDesc = output.Description;
            this.mTextureDesc = new Texture2DDescription()
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = this.mOutputDesc.DesktopBounds.Width,
                Height = this.mOutputDesc.DesktopBounds.Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            try
            {
                this.mDeskDupl = output1.DuplicateOutput(mDevice);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.NotCurrentlyAvailable.Result.Code)
                {
                    throw new DesktopDuplicationException("There is already the maximum number of applications using the Desktop Duplication API running, please close one of the applications and try again.");
                }
            }

        }

        public DesktopFrame GetLatestFrame()
        {

            var frame = new DesktopFrame();
            // Try to get the latest frame; this may timeout
            bool retrievalTimedOut = RetrieveFrame();
            if (retrievalTimedOut)
                return null;
            try
            {
                // RetrieveFrameMetadata(frame);
                //   RetrieveCursorMetadata(frame);

                ProcessFrame(frame);

            }
            catch
            {
                ReleaseFrame();
            }
            try
            {
                ReleaseFrame();
            }
            catch
            {
                //    throw new DesktopDuplicationException("Couldn't release frame.");  
            }

            return frame;
        }

        private bool RetrieveFrame()
        {
            if (desktopImageTexture == null)
                desktopImageTexture = new Texture2D(mDevice, mTextureDesc);
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
                if (ex.ResultCode.Failure)
                {
                    throw new DesktopDuplicationException("Failed to acquire next frame.");
                }
            }
            using (var tempTexture = desktopResource.QueryInterface<Texture2D>())
                mDevice.ImmediateContext.CopyResource(tempTexture, desktopImageTexture);
            desktopResource.Dispose();
            return false;
        }

        private void RetrieveFrameMetadata(DesktopFrame frame)
        {

            if (frameInfo.TotalMetadataBufferSize > 0)
            {
                // Get moved regions
                int movedRegionsLength = 0;
                OutputDuplicateMoveRectangle[] movedRectangles = new OutputDuplicateMoveRectangle[frameInfo.TotalMetadataBufferSize];
                mDeskDupl.GetFrameMoveRects(movedRectangles.Length, movedRectangles, out movedRegionsLength);
                frame.MovedRegions = new MovedRegion[movedRegionsLength / Marshal.SizeOf(typeof(OutputDuplicateMoveRectangle))];
                for (int i = 0; i < frame.MovedRegions.Length; i++)
                {
                    frame.MovedRegions[i] = new MovedRegion()
                    {
                        Source = new System.Drawing.Point(movedRectangles[i].SourcePoint.X, movedRectangles[i].SourcePoint.Y),
                        Destination = new System.Drawing.Rectangle(movedRectangles[i].DestinationRect.X, movedRectangles[i].DestinationRect.Y, movedRectangles[i].DestinationRect.Width, movedRectangles[i].DestinationRect.Height)
                    };
                }

                // Get dirty regions
                int dirtyRegionsLength = 0;
                Rectangle[] dirtyRectangles = new Rectangle[frameInfo.TotalMetadataBufferSize];
                mDeskDupl.GetFrameDirtyRects(dirtyRectangles.Length, dirtyRectangles, out dirtyRegionsLength);
                frame.UpdatedRegions = new System.Drawing.Rectangle[dirtyRegionsLength / Marshal.SizeOf(typeof(Rectangle))];
                for (int i = 0; i < frame.UpdatedRegions.Length; i++)
                {
                    frame.UpdatedRegions[i] = new System.Drawing.Rectangle(dirtyRectangles[i].X, dirtyRectangles[i].Y, dirtyRectangles[i].Width, dirtyRectangles[i].Height);
                }
            }
            else
            {
                frame.MovedRegions = new MovedRegion[0];
                frame.UpdatedRegions = new System.Drawing.Rectangle[0];
            }
        }

        private void ProcessFrame(DesktopFrame frame)
        {

            // Get the desktop capture texture
            var mapSource = mDevice.ImmediateContext.MapSubresource(desktopImageTexture, 0, MapMode.Read, MapFlags.None);

            Bitmap newImage;
          //  if (!_allocatedBitmaps.TryDequeue(out newImage))
            {
                newImage = new System.Drawing.Bitmap(mOutputDesc.DesktopBounds.Width, mOutputDesc.DesktopBounds.Height, PixelFormat.Format32bppRgb);
            }
            FinalImage = newImage;

            var boundsRect = new System.Drawing.Rectangle(0, 0, mOutputDesc.DesktopBounds.Width, mOutputDesc.DesktopBounds.Height);
            // Copy pixels from screen capture Texture to GDI bitmap
            var mapDest = FinalImage.LockBits(boundsRect, ImageLockMode.WriteOnly, FinalImage.PixelFormat);
            var sourcePtr = mapSource.DataPointer;
            var destPtr = mapDest.Scan0;


            for (int y = 0; y < mOutputDesc.DesktopBounds.Height; y++)
            {
                // Copy a single line 
                Utilities.CopyMemory(destPtr, sourcePtr, mOutputDesc.DesktopBounds.Width * 4);

                // Advance pointers
                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);

            }

            // Release source and dest locks
            FinalImage.UnlockBits(mapDest);
            mDevice.ImmediateContext.UnmapSubresource(desktopImageTexture, 0);
            frame.DesktopImage = FinalImage;
        }

        private void ReleaseFrame()
        {
            try
            {
                mDeskDupl.ReleaseFrame();
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Failure)
                {
                    throw new DesktopDuplicationException("Failed to release frame.");
                }
            }
        }
    }
}



