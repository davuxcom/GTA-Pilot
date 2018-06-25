using System;
using System.ComponentModel;
using System.Windows.Input;

namespace GTAPilot
{

    public class RelayCommand : ICommand
    {
        private Action _actionToExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action actionToExecute)
        {
            _actionToExecute = actionToExecute;
        }

        public bool CanExecute(object parameter = null)
        {
            return true;
        }

        public void Execute(object parameter = null)
        {
            if (_actionToExecute == null)
            {
                return;
            }

            _actionToExecute.Invoke();
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, null);
            }
        }
    }

    public class ModeControlPanel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public RelayCommand HeadingToggle { get; }
        public RelayCommand IASToggle { get; }
        public RelayCommand BankToggle { get; }
        public RelayCommand ALTToggle { get; }
        public RelayCommand VSToggle { get; }
        public RelayCommand LNAVToggle { get; }

        private bool _pitchHold;
        public bool VSHold
        {
            get => _pitchHold;
            set => UpdateAndNotify(nameof(VSHold), value, ref _pitchHold);
        }

        private bool _lnav;
        public bool LNAV
        {
            get => _lnav;
            set => UpdateAndNotify(nameof(LNAV), value, ref _lnav);
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

        private bool _iasHold;

        public bool IASHold
        {
            get => _iasHold;
            set => UpdateAndNotify(nameof(IASHold), value, ref _iasHold);
        }

        private bool _altitudeHold;
        public bool AltitudeHold
        {
            get => _altitudeHold;
            set => UpdateAndNotify(nameof(AltitudeHold), value, ref _altitudeHold);
        }

        private double _vs;
        public double VS
        {
            get => _vs;
            set => UpdateAndNotify(nameof(VS), value, ref _vs);
        }

        private double _bank;
        public double Bank
        {
            get => _bank;
            set => UpdateAndNotify(nameof(Bank), value, ref _bank);
        }
        private double _ias;
        public double IAS
        {
            get => _ias;
            set => UpdateAndNotify(nameof(IAS), value, ref _ias);
        }

        private double _alt;
        public double ALT
        {
            get => _alt;
            set => UpdateAndNotify(nameof(ALT), value, ref _alt);
        }

        private double _hdg;
        public double HDG
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

        public ModeControlPanel()
        {
            HeadingToggle = new RelayCommand(() => HeadingHold = !HeadingHold);
            IASToggle = new RelayCommand(() => IASHold = !IASHold);
            BankToggle = new RelayCommand(() => BankHold = !BankHold);
            ALTToggle = new RelayCommand(() => AltitudeHold = !AltitudeHold);
            VSToggle = new RelayCommand(() => VSHold = !VSHold);
            LNAVToggle = new RelayCommand(() => LNAV = !LNAV);
        }
    }
}
