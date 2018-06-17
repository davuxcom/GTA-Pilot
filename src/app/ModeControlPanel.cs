using System.ComponentModel;

namespace GTAPilot
{
    class ModeControlPanel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _pitchHold;
        public bool VSHold
        {
            get => _pitchHold;
            set => UpdateAndNotify(nameof(VSHold), value, ref _pitchHold);
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

        private bool _altitudeHold;
        public bool AltitudeHold
        {
            get => _altitudeHold;
            set => UpdateAndNotify(nameof(AltitudeHold), value, ref _altitudeHold);
        }

        private int _vs;
        public int VS
        {
            get => _vs;
            set => UpdateAndNotify(nameof(VS), value, ref _vs);
        }

        private int _bank;
        public int Bank
        {
            get => _bank;
            set => UpdateAndNotify(nameof(Bank), value, ref _bank);
        }
        private int _ias;
        public int IAS
        {
            get => _ias;
            set => UpdateAndNotify(nameof(IAS), value, ref _ias);
        }

        private int _alt;
        public int ALT
        {
            get => _alt;
            set => UpdateAndNotify(nameof(ALT), value, ref _alt);
        }

        private int _hdg;
        public int HDG
        {
            get => _hdg;
            set => UpdateAndNotify(nameof(HDG), value, ref _hdg);
        }

        private void UpdateAndNotify<T>(string prop, T newValue, ref T storageValue)
        {
            if (!newValue.Equals(storageValue))
            {
                storageValue = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
