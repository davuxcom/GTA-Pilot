using System.Windows.Controls;

namespace GTAPilot
{
    public partial class FlightPlanControl : UserControl
    {
        public RelayCommand Open { get; }
        public RelayCommand Save { get; }
        public RelayCommand Edit { get; }

     //   public ObservableCollection<Points>

        public FlightPlanControl()
        {
            InitializeComponent();
        }
    }
}
