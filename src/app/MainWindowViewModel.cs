using GTAPilot.Extensions;
using GTAPilot.Indicators_v2;
using System;
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
        public double Value => Math.Round(_indicator.Value, 1);
        public ImageSource Img => ((Bitmap)_indicator.Image?.ToBitmap()).ToImageSource();

        Indicator _indicator;

        public IndicatorViewModel(string name, Indicator indicator)
        {
            Name = name;
            _indicator = indicator;
        }

        public void Tick()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
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

        SystemManager _inputManager;

        public MainWindowViewModel(SystemManager inputManager)
        {
            _inputManager = inputManager;

            Counters = new ObservableCollection<FpsCounterViewModel>();

            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Capture_Enqueue), "Capture"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Capture_Dequeue), "Dispatch"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Roll), "Roll"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Pitch), "Pitch"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Airspeed), "Airspeed"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Altitude), "Altitude"));
            Counters.Add(new FpsCounterViewModel(_inputManager.GetCounter(FpsCounterType.Yaw), "Yaw"));

            Indicators = new ObservableCollection<IndicatorViewModel>();
            Indicators.Add(new IndicatorViewModel("Roll", _inputManager.IndicatorHost.Roll));
            Indicators.Add(new IndicatorViewModel("Pitch", _inputManager.IndicatorHost.Pitch));
            Indicators.Add(new IndicatorViewModel("Airspeed", _inputManager.IndicatorHost.Airspeed));
            Indicators.Add(new IndicatorViewModel("Altitude", _inputManager.IndicatorHost.Altitude));
            Indicators.Add(new IndicatorViewModel("Yaw", _inputManager.IndicatorHost.Compass));
        }

        public void Tick()
        {
            foreach (var c in Counters) c.Tick();
            foreach (var i in Indicators) i.Tick();
        }

        internal void Save()
        {
            _inputManager.SaveAll();
        }
    }
}
