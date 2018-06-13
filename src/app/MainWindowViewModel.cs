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
    class IndicatorViewModel : INotifyPropertyChanged
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

        Indicator _indicator;

        public IndicatorViewModel(string name, Indicator indicator)
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

    class FpsCounterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int FPS => _counter.CalculateFps();
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

    class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FpsCounterViewModel> Counters { get; }
        public ObservableCollection<IndicatorViewModel> Indicators { get; }

        SaveFrameConsumer _captureSink;
        SystemManager _inputManager;

        public MainWindowViewModel(SystemManager inputManager, SaveFrameConsumer captureSink)
        {
            _captureSink = captureSink;
            _inputManager = inputManager;

            Counters = new ObservableCollection<FpsCounterViewModel>();
            Indicators = new ObservableCollection<IndicatorViewModel>();

            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Capture_Enqueue), "Capture"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Capture_Dequeue), "Dispatch"));

            if (_captureSink != null)
            {
                Counters.Add(new FpsCounterViewModel(_captureSink.FPS, "Flush"));
            }
            else
            {
                Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Roll), "Roll"));
                Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Pitch), "Pitch"));
                Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Airspeed), "Airspeed"));
                Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Altitude), "Altitude"));
                Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Yaw), "Yaw"));


                Indicators.Add(new IndicatorViewModel("Roll", _inputManager.IndicatorHost.Roll));
                Indicators.Add(new IndicatorViewModel("Pitch", _inputManager.IndicatorHost.Pitch));
                Indicators.Add(new IndicatorViewModel("Airspeed", _inputManager.IndicatorHost.Airspeed));
                Indicators.Add(new IndicatorViewModel("Altitude", _inputManager.IndicatorHost.Altitude));
                Indicators.Add(new IndicatorViewModel("Yaw", _inputManager.IndicatorHost.Compass));
            }
        }

        public void Tick()
        {
            foreach (var c in Counters) c.Tick();
            foreach (var i in Indicators) i.Tick();
        }
    }
}
