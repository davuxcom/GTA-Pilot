using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GTAPilot
{
    class PerThreadUtils
    {
        static ConcurrentDictionary<int, Tesseract> Tessreacts = new ConcurrentDictionary<int, Tesseract>();
        static ConcurrentDictionary<int, CvBlobDetector> BlobDetectors = new ConcurrentDictionary<int, CvBlobDetector>();

        private static CvBlobDetector CreateBlobDetector()
        {
            return new CvBlobDetector();
        }

        public static CvBlobDetector GetBlobDetector()
        {
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

            if (!BlobDetectors.Keys.Contains(tid))
            {
                BlobDetectors.TryAdd(tid, CreateBlobDetector());
            }
            return BlobDetectors[tid];
        }

        private static Tesseract CreateTesseract()
        {
            var ocr = new Tesseract();
            ocr.Init("", "eng", OcrEngineMode.TesseractOnly);
            return ocr;
        }

        public static Tesseract GetTesseract()
        {
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

            if (!Tessreacts.Keys.Contains(tid))
            {
                Tessreacts.TryAdd(tid, CreateTesseract());
            }
            return Tessreacts[tid];
        }
    }
}
