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

        public RelayCommand Open { get; }
        public RelayCommand Save { get; }
        public RelayCommand Edit { get; }
        public ObservableCollection<System.Drawing.PointF> Points => _plan.Points;

        private FlightPlan _plan;

        public FlightPlanControl()
        {
            InitializeComponent();

            _plan = SystemManager.Instance.FlightPlan;
            _plan.PropertyChanged += FlightPlan_PropertyChanged;

            Edit = new RelayCommand(() =>
            {
                var editor = new FlightPlanBuidler(_plan.Points.ToArray());
                editor.ShowDialog();

                _plan.Load(editor.Points);
            });

            Save = new RelayCommand(() =>
            {
                StringBuilder ret = new StringBuilder();
                foreach (var p in _plan.Points)
                {
                    ret.AppendLine($"{p.X},{p.Y}");
                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "FlightPlan";
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";

                if (dlg.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dlg.FileName, ret.ToString());
                }
            });

            Open = new RelayCommand(() =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = "FlightPlan";
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";

                if (dlg.ShowDialog() == true)
                {
                    SystemManager.Instance.FlightPlan.LoadFromFile(dlg.FileName);
                }
            });

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