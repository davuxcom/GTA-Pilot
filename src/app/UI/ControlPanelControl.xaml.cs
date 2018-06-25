using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace GTAPilot
{
    public partial class ControlPanelControl : UserControl, ICanTick
    {
        public ObservableCollection<IndicatorViewModel> Indicators { get; }

        public ControlPanelControl()
        {
            InitializeComponent();
            App.Register(this);

            Indicators = new ObservableCollection<IndicatorViewModel>();
            Indicators.Add(new IndicatorViewModel("Roll", SystemManager.Instance.IndicatorHost.Roll));
            Indicators.Add(new IndicatorViewModel("Pitch", SystemManager.Instance.IndicatorHost.Pitch));
            Indicators.Add(new IndicatorViewModel("Speed", SystemManager.Instance.IndicatorHost.Airspeed));
            Indicators.Add(new IndicatorViewModel("Altitude", SystemManager.Instance.IndicatorHost.Altitude));
            Indicators.Add(new IndicatorViewModel("Yaw", SystemManager.Instance.IndicatorHost.Compass));

            DataContext = this;
        }

        public void Tick()
        {
            foreach (var i in Indicators) i.Tick();
        }
    }
}
