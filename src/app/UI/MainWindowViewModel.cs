using GTAPilot.Extensions;
using GTAPilot.Indicators_v2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public class FpsCounterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int FPS => _counter.Fps;
        public string Name { get; private set; }

        FpsCounter _counter;

        public FpsCounterViewModel(FpsCounter counter, string name)
        {
            _counter = counter;
            Name = name;
        }

        public void Tick()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FPS)));
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FpsCounterViewModel> Counters { get; }
        public ObservableCollection<IndicatorViewModel> Indicators { get; }

        public PID.Gain RollPID => FlightComputerConfig.Roll.Gain;
        public PID.Gain VSPID => FlightComputerConfig.Pitch.Gain;

        internal MainWindowViewModel()
        {

            Counters = new ObservableCollection<FpsCounterViewModel>();
            Indicators = new ObservableCollection<IndicatorViewModel>();

            Counters.Add(new FpsCounterViewModel(SystemManager.Instance.Capture, "Capture"));

            /*
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Roll), "Roll"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Pitch), "Pitch"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Airspeed), "Speed"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Altitude), "Altitude"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Yaw), "Yaw"));
            */


            Counters.Add(new FpsCounterViewModel(SystemManager.Instance.App.Controller.XInput_In, "XInput-In"));
            Counters.Add(new FpsCounterViewModel(SystemManager.Instance.App.Controller.XInput_Out, "XInput-Out"));



            Indicators.Add(new IndicatorViewModel("Roll", SystemManager.Instance.IndicatorHost.Roll));
            Indicators.Add(new IndicatorViewModel("Pitch", SystemManager.Instance.IndicatorHost.Pitch));
            Indicators.Add(new IndicatorViewModel("Speed", SystemManager.Instance.IndicatorHost.Airspeed));
            Indicators.Add(new IndicatorViewModel("Altitude", SystemManager.Instance.IndicatorHost.Altitude));
            Indicators.Add(new IndicatorViewModel("Yaw", SystemManager.Instance.IndicatorHost.Compass));
        }

        public void Tick()
        {
            foreach (var c in Counters) c.Tick();
            foreach (var i in Indicators) i.Tick();
        }
    }
}
