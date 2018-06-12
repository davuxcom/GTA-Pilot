using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GTAPilot
{
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

        public ImageSource RollImg { get; private set; }
        public ImageSource PitchImg { get; private set; }
        public ImageSource SpeedImg { get; private set; }
        public ImageSource AltitudeImg { get; private set; }
        public ImageSource CompassImg { get; private set; }


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
        }

        public void Tick()
        {
            foreach (var c in Counters) c.Tick();

            RollImg = _inputManager.GetLatestFrame(FpsCounterType.Roll).ToImageSource();
            PitchImg = _inputManager.GetLatestFrame(FpsCounterType.Pitch).ToImageSource();
            SpeedImg = _inputManager.GetLatestFrame(FpsCounterType.Airspeed).ToImageSource();
            AltitudeImg = _inputManager.GetLatestFrame(FpsCounterType.Altitude).ToImageSource();
            CompassImg = _inputManager.GetLatestFrame(FpsCounterType.Yaw).ToImageSource();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RollImg)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PitchImg)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeedImg)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AltitudeImg)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompassImg)));

        }
    }
}
