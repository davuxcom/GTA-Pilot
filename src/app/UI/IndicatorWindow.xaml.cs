using System.Windows;

namespace GTAPilot
{
    public partial class IndicatorWindow : Window
    {
        IndicatorViewModel _viewModel;
        internal IndicatorWindow(IndicatorViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = _viewModel;
        }
    }
}
