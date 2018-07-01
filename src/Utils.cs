using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;

namespace GTAPilot
{
    class Utils
    {
        public static Image<Gray,byte> RemoveBlobs(Image<Gray,byte> img, int minSize, int maxSize)
        {
            var blobs = PerThreadUtils.DetectAndFilterBlobs(img, minSize, maxSize);

            Mat blobMask = new Mat(img.Size, DepthType.Cv8U, 3);
            blobMask.SetTo(new Bgr(Color.White).MCvScalar);

            foreach (var b in blobs) CvInvoke.Rectangle(blobMask, b.BoundingBox, new Bgr(Color.Black).MCvScalar, -1);

            return img.Copy(blobMask.ToImage<Gray, byte>());
        }
    }
}
