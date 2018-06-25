using GTAPilot.Extensions;
using GTAPilot.Indicators_v2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Media;

namespace GTAPilot
{
    public enum IndicatorType
    {
        Roll, Pitch, Speed, Altitude, Yaw,
    }


    public class IndicatorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; private set; }
        public double Value => Math.Round(_indicator.LastGoodValue, 1);
        public double BadFrameCount => _indicator.BadFrames.Count;
        public double CachedTuningValue => _indicator.CachedTuningValue;
        public ImageSource Img => ((Bitmap)_indicator.Image[0]?.ToBitmap()).ToImageSource();
        public ImageSource Img2 => ((Bitmap)_indicator.Image[1]?.ToBitmap()).ToImageSource();
        public ImageSource Img3 => ((Bitmap)_indicator.Image[2]?.ToBitmap()).ToImageSource();
        public ImageSource Img4 => ((Bitmap)_indicator.Image[3]?.ToBitmap()).ToImageSource();

        public HashSet<int> BadFrames => _indicator.BadFrames;

        public IndicatorType Type
        {
            get
            {
                switch (Name)
                {
                    case "Roll": return IndicatorType.Roll;
                    case "Pitch": return IndicatorType.Pitch;
                    case "Speed": return IndicatorType.Speed;
                    case "Altitude": return IndicatorType.Altitude;
                    case "Yaw": return IndicatorType.Yaw;
                    default: throw new NotImplementedException();
                }
            }
        }

        Indicator _indicator;

        internal IndicatorViewModel(string name, Indicator indicator)
        {
            Name = name;
            _indicator = indicator;
        }

        public void Tick()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img2)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img3)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img4)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BadFrameCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CachedTuningValue)));
        }
    }

}
