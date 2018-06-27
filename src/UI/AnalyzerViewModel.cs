using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GTAPilot
{
    public class AnalyzerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FpsCounterViewModel> Counters { get; }
        public ObservableCollection<IndicatorViewModel> Indicators { get; }

        public RelayCommand PlayPause { get; }
        public int FrameCount => SystemManager.Instance.Replay != null ? SystemManager.Instance.Replay.FrameCount : 0;
        public int CurrentFrame => SystemManager.Instance.Replay != null ? SystemManager.Instance.Replay.CurrentFrame : 0;

        public PID.Gain RollPID => FlightComputerConfig.Roll.Gain;
        public PID.Gain VSPID => FlightComputerConfig.Pitch.Gain;

        internal AnalyzerViewModel()
        {
            Counters = new ObservableCollection<FpsCounterViewModel>();
            Indicators = new ObservableCollection<IndicatorViewModel>();

            Counters.Add(new FpsCounterViewModel(SystemManager.Instance.Capture, "Capture"));
            if (!SystemManager.Instance.IsReplay)
            {
                Counters.Add(new FpsCounterViewModel(SystemManager.Instance.App.Controller.XInput_In, "XInput-In"));
                Counters.Add(new FpsCounterViewModel(SystemManager.Instance.App.Controller.XInput_Out, "XInput-Out"));
            }
            else
            {
                PlayPause = new RelayCommand(SystemManager.Instance.Replay.PlayPause);
                SystemManager.Instance.Replay.PropertyChanged += Replay_PropertyChanged;
            }

            Indicators.Add(new IndicatorViewModel("Roll", SystemManager.Instance.IndicatorHost.Roll));
            Indicators.Add(new IndicatorViewModel("Pitch", SystemManager.Instance.IndicatorHost.Pitch));
            Indicators.Add(new IndicatorViewModel("Speed", SystemManager.Instance.IndicatorHost.Airspeed));
            Indicators.Add(new IndicatorViewModel("Altitude", SystemManager.Instance.IndicatorHost.Altitude));
            Indicators.Add(new IndicatorViewModel("Yaw", SystemManager.Instance.IndicatorHost.Compass));
        }

        private void Replay_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        public void Tick()
        {
            foreach (var c in Counters) c.Tick();
            foreach (var i in Indicators) i.Tick();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFrame)));
        }
    }
}
