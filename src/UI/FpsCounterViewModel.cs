using System.ComponentModel;

namespace GTAPilot
{
    public class FpsCounterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int FPS => _counter.Fps;
        public string Name { get; }

        private FpsCounter _counter;

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
}
