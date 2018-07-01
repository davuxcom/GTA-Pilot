using System;
using System.Windows;
using System.Windows.Controls;

namespace GTAPilot
{
    public partial class IndicatorWindow : Window, ICanTick
    {
        IndicatorViewModel _viewModel;
        internal IndicatorWindow(IndicatorViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = _viewModel;

            App.Register(this);
        }

        public void Tick()
        {
            _viewModel.Tick();
        }

        private void TextBox_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var txt = (TextBox)sender;

            double next = 0;

            var vl = int.Parse(txt.Text);

            next = (vl + Math.Sign(e.Delta) * 1);


            txt.Text = next.ToString();
        }
    }
}
