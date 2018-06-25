using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace GTAPilot
{
    public partial class FlightPlanControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        public ObservableCollection<System.Drawing.PointF> Points => _plan.Points;

        private FlightPlan _plan;

        public FlightPlanControl()
        {
            InitializeComponent();

            _plan = SystemManager.Instance.FlightPlan;
            _plan.PropertyChanged += FlightPlan_PropertyChanged;

            LoadFlightPlanPoints();

            DataContext = this;
        }

        private void LoadFlightPlanPoints()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
        }

        private void FlightPlan_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_plan.Points))
            {
                LoadFlightPlanPoints();
                UpdateLayout();
            }
            else if (e.PropertyName == nameof(_plan.CurrentIndex))
            {
                lstPoints.SelectedIndex = _plan.CurrentIndex;
                lstPoints.ScrollIntoView(lstPoints.SelectedItem);
            }
        }
    }
}