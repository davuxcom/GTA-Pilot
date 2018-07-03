using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.Drawing;

namespace GTAPilot
{
    class Utils
    {
        public static Image<Gray, byte> RemoveBlobs(Image<Gray, byte> img, int minSize, int maxSize)
        {
            var blobs = DetectAndFilterBlobs(img, minSize, maxSize);

            Mat blobMask = new Mat(img.Size, DepthType.Cv8U, 3);
            blobMask.SetTo(new Bgr(Color.White).MCvScalar);

            foreach (var b in blobs) CvInvoke.Rectangle(blobMask, b.BoundingBox, new Bgr(Color.Black).MCvScalar, -1);

            return img.Copy(blobMask.ToImage<Gray, byte>());
        }

        public static Image<Gray, byte> RemoveAllButBlobs(Image<Gray, byte> img, IEnumerable<CvBlob> blobs)
        {
            Mat blobMask = new Mat(img.Size, DepthType.Cv8U, 3);
            blobMask.SetTo(new MCvScalar(1));

            foreach (var b in blobs) CvInvoke.Rectangle(blobMask, b.BoundingBox, new Bgr(Color.White).MCvScalar, -1);

            return img.Copy(blobMask.ToImage<Gray, byte>());
        }

        public static IEnumerable<CvBlob> DetectAndFilterBlobs(Image<Gray, byte> img, int min, int max)
        {
            CvBlobs blobs = new CvBlobs();
            PerThreadUtils.GetBlobDetector().Detect(img, blobs);
            blobs.FilterByArea(min, max);
            return blobs.Values;
        }



        public static string ReadTextFromImage(Image<Bgr, byte> img, DebugState debugState)
        {
            return ReadTextFromImage(img.Convert<Hsv, byte>()[2], debugState);
        }

        public static string ReadTextFromImage(Image<Gray, byte> img, DebugState debugState)
        {
            debugState.Add(img);

            var ocr = PerThreadUtils.GetTesseract();
            ocr.SetImage(img);
            ocr.Recognize();

            var str = "";
            foreach (var c in ocr.GetCharacters())
            {
                str += c.Text.Trim().ToUpper();
            }
            return str;
        }
    }
}
