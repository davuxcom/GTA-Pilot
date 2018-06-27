using GTAPilot.Extensions;
using GTAPilot.Indicators;
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
        public ImageSource[] Img { get; }

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

        private Indicator _indicator;

        internal IndicatorViewModel(string name, Indicator indicator)
        {
            Name = name;
            _indicator = indicator;

            Img = new ImageSource[] { null, null, null, null, null };
        }

        public void Tick()
        {
            if (SystemManager.Instance.IsReplay)
            {
                for(var i = 0; i < 5; i++)
                {
                    Img[i] = ((Bitmap)_indicator.Image[i]?.ToBitmap()).ToImageSource();
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BadFrameCount)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CachedTuningValue)));
            }
        }
    }
}
