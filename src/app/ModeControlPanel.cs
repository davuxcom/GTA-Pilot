using System.ComponentModel;

namespace GTAPilot
{
    class ModeControlPanel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _pitchHold;
        public bool PitchHold
        {
            get => _pitchHold;
            set => UpdateAndNotify(nameof(PitchHold), value, ref _pitchHold);
        }

        private bool _bankHold;
        public bool BankHold
        {
            get => _bankHold;
            set => UpdateAndNotify(nameof(BankHold), value, ref _bankHold);
        }

        private bool _headingHold;


        public bool HeadingHold
        {
            get => _headingHold;
            set => UpdateAndNotify(nameof(HeadingHold), value, ref _headingHold);
        }

        private bool _speedHold;

        public bool SpeedHold
        {
            get => _speedHold;
            set => UpdateAndNotify(nameof(SpeedHold), value, ref _speedHold);
        }

        private void UpdateAndNotify(string prop, bool newValue, ref bool storageValue)
        {
            if (newValue != storageValue)
            {
                storageValue = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
