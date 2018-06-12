using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using GTAPilot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

public class Hints
{
    public static CircleF AttitudeIndicator;
    public static CircleF PitchIndicator;
    public static CircleF SpeedIndicator;
    public static System.Drawing.Rectangle PitchRect;
}

public class IndicatorData
{
    public static Size FrameSize = new Size(1920, 1200);

    public Image<Bgr, byte> Frame;
    public DateTime Timestamp;
    public long Id;

    public bool IsFastROI;
    public Rectangle ROI;
}

public abstract class Indicator
{
    public class IndicatorValueData
    {
        public long Tick;
        public double Value;
    }

    public List<IndicatorValueData> InputValues = new List<IndicatorValueData>();
    public List<IndicatorValueData> InputValues2 = new List<IndicatorValueData>();
    public List<IndicatorValueData> OutputValues = new List<IndicatorValueData>();
    public List<IndicatorValueData> SetpointValues = new List<IndicatorValueData>();

    public int CurrentTuningValue = 0;
    public List<Hsv> TuningValues = new List<Hsv>();

    public Hsv TuningValue { get { return TuningValues[CurrentTuningValue]; } }
    public string LastAction;
    public DateTime LastGoodFrameTime;

    List<double> LastValues = new List<double>();

    public double LastGoodValue = -1;


    protected virtual double AverageCore(double[] values)
    {
        return values.Sum() / values.Length;
    }


    double GetAverage(TimeSpan delta)
    {
        var l_id = LastGoodFrameId;
        var l_time = LastGoodFrameTime;

        if (LastGoodFrameTime == default(DateTime)) return double.NaN;

        List<double> retValues = new List<double>();

        var captureTime = l_time + delta;

        
        while (Timeline.Data[l_id].Time >= captureTime)
        {
            var value = Timeline.Data[l_id][Type];

            if (!double.IsNaN(value))
            {
                retValues.Add(value);
            }

            if (l_id == 0) break;
            l_id--;
        }

        return AverageCore(retValues.ToArray());
    }

    public double LastGoodValueAverage
    {
        get
        {
            return GetAverage(TimeSpan.FromMilliseconds(-250));
        }
    }

    public double LastGoodValueAverage2
    {
        get
        {

            return GetAverage(TimeSpan.FromMilliseconds(-250));



        }
    }

    protected virtual Rectangle FastROI
    {
        get
        {
            return default(Rectangle);
        }
    }

    protected virtual Rectangle SlowROI
    {
        get
        {
            var width = 1920;
            var height = 1200;
            var new_rect = new Rectangle(350, 200, width - 900, height - 200);
            return new_rect;
        }
    }

    public FpsCounter Counter = new FpsCounter();

    public Image<Bgr, byte> IntermediateFrameBgr;
    public Image<Gray, byte> IntermediateFrameGray;

    public Image<Bgr, byte> BestIntermediate
    {
        get
        {
            return IntermediateFrameBgr != null ? IntermediateFrameBgr : (IntermediateFrameGray == null ? null : IntermediateFrameGray.Convert<Bgr, byte>());
        }
    }

    public string Name
    {
        get
        {
            return Type.ToString();
        }
    }

    int failedFrameCounter = -500;
    int good_frames_on_cv = 0;

    public ConcurrentQueue<IndicatorData> missedFrames = new ConcurrentQueue<IndicatorData>();

    volatile IndicatorData _nextData;

    public Indicator()
    {
        for (int i = 0; i < 20; ++i) LastValues.Add(0);

        new System.Threading.Thread(() =>
        {
            while (true)
            {
                if (!IndicatorThreadFrame())
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
        }).Start();

        var thread2 = new System.Threading.Thread(() =>
        {

            while (true)
            {
                if (!IndicatorThreadFrame2())
                {
                    System.Threading.Thread.Sleep(10);
                }
            }
        });
        thread2.Priority = System.Threading.ThreadPriority.Lowest;
        thread2.Start();
    }

    private bool IndicatorThreadFrame()
    {
        IndicatorData nextData = null;
        try
        {
            nextData = System.Threading.Interlocked.Exchange(ref _nextData, null);
            if (nextData != null)
            {
                TickImpl(nextData);
                return true;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine("INDC: " + ex);
        }
        return false;
    }

    private bool IndicatorThreadFrame2()
    {
        IndicatorData nextData = null;
        try
        {
            if (missedFrames.TryDequeue(out nextData))
            {
                switch (this.Type)
                {
                    case InputType.Yaw:
                    case InputType.Speed:
                        TickImpl(nextData);
                        break;
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine("INDC: " + ex);
        }
        return false;
    }

    public bool Tick(IndicatorData data)
    {
        try
        {
            var retData = new IndicatorData
            {
                Timestamp = data.Timestamp,
                Id = data.Id,
            };

            var area = FastROI;
            if (area == default(Rectangle))
            {
                area = SlowROI;
                retData.IsFastROI = false;
            }
            else
            {
                retData.IsFastROI = true;
            }

            retData.Frame = data.Frame.Copy(area);
            retData.ROI = area;

            var lostFrame = System.Threading.Interlocked.Exchange(ref _nextData, retData);

           // if (_panel.GPS.LastTime_Input <= data.Timestamp)
            {
                if (lostFrame != null) missedFrames.Enqueue(lostFrame);
            }
                
        }
        catch (Exception ex)
        {
            Trace.WriteLine("tick failed " + ex);
        }
        return true;
    }

    public long LastGoodFrameId = 0;

    protected ConcurrentDictionary<long, IndicatorData> SavedFrames = new ConcurrentDictionary<long, IndicatorData>();

    public bool TickImpl(IndicatorData data)
    {
        try
        {
            if (Type == InputType.Yaw)
            {
              //  SavedFrames.TryAdd(data.Id, data);
            }
            var IsValidInput = true; // (_panel.GPS.LastTime_Input <= data.Timestamp); ;

          //  if (!IsValidInput && Type != InputType.Menu) return true;

            double ObservedValue = 0;
            if (ProcessFrameCore(data, ref ObservedValue))
            {
                if (data.Id > LastGoodFrameId)
                {
                    LastGoodFrameTime = data.Timestamp;
                    LastGoodFrameId = data.Id;
                    LastGoodValue = ObservedValue;
                }
                good_frames_on_cv++;
                failedFrameCounter = 0;

                OnSuccessFrame(data, ref ObservedValue);

                var frame_data = Timeline.Data[data.Id];

                if (IsValidInput)
                {
                    switch (this.Type)
                    {
                        case InputType.Roll: frame_data.Roll = ObservedValue; break;
                        case InputType.Pitch: frame_data.Pitch = ObservedValue; break;
                        case InputType.Yaw: frame_data.Heading = ObservedValue; break;
                        case InputType.Altitude: frame_data.Altitude = ObservedValue; break;
                        case InputType.Speed:
                            frame_data.Speed = ObservedValue;
                            frame_data.Speed2 = LastGoodValueAverage;
                            break;
                    }
                }
                
                Counter.GotFrame();
                return true;
            }
            else
            {
                if (IsValidInput)
                {

                    if (Type == InputType.Yaw)
                    {
                        Trace.WriteLine("Failed compass " + LastAction + " " + good_frames_on_cv);
                        //SavedFrames.TryAdd(data.Id, data);

                    }



                    failedFrameCounter++;
                    if (failedFrameCounter > 30)
                    {
                        Trace.WriteLine("#### TUNE " + Type + " " + CurrentTuningValue);

                        good_frames_on_cv = 0;

                        CurrentTuningValue++;
                        if (CurrentTuningValue >= TuningValues.Count)
                        {
                            CurrentTuningValue = 0;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("TickImpl: " + ex);
        }
        return false;
    }

    public void SaveImages()
    {
        var c = SavedFrames.Count;

        

        for (var i = 0; i < c; ++i)
        {
            var d = SavedFrames.Values.ElementAt(i);

            d.Frame.Save(string.Format("d:\\imgs\\img-saved_{0:HH-mm-ss.fff}.png", d.Timestamp));
        }
    }

    protected abstract bool ProcessFrameCore(IndicatorData data, ref double ObservedValue);
    protected abstract InputType Type { get; }


    void OnSuccessFrame(IndicatorData data, ref double ObservedValue)
    {
        /*
        if (data.Timestamp >= _panel.GPS.LastTime_Input)
        {
            if (Type != InputType.Yaw)
            {
                InputValues.Add(new IndicatorValueData { Tick = _panel.Ticks, Value = ObservedValue });
                InputValues2.Add(new IndicatorValueData { Tick = _panel.Ticks, Value = LastGoodValueAverage });
            }
        }
        */
        OnSuccessFrameCore(data, ref ObservedValue);
    }

    protected virtual void OnSuccessFrameCore(IndicatorData data, ref double ObservedValue)
    {
        
    }


    ConcurrentDictionary<int, Tesseract> Tessreacts = new ConcurrentDictionary<int, Tesseract>();

    Tesseract CreateTesseract()
    {
        var ocr = new Tesseract();
        ocr.Init("", "eng", OcrEngineMode.TesseractOnly);
        return ocr;
    }

    protected Tesseract GetTesseract()
    {
        var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

        if (!Tessreacts.Keys.Contains(tid))
        {
            Tessreacts.TryAdd(tid, CreateTesseract());
        }
        return Tessreacts[tid];
    }


    ConcurrentDictionary<int, CvBlobDetector> BlobDetectors = new ConcurrentDictionary<int, CvBlobDetector>();

    CvBlobDetector CrateBlobDetector()
    {
        return new CvBlobDetector();
    }

    protected CvBlobDetector GetBlobDetector()
    {
        var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

        if (!BlobDetectors.Keys.Contains(tid))
        {
            BlobDetectors.TryAdd(tid, CrateBlobDetector());
        }
        return BlobDetectors[tid];
    }

}
