using System.Windows;
using System.Windows.Controls;

namespace GTAPilot
{
    public partial class EICASControl : UserControl, ICanTick
    {
        public EICASControl()
        {
            InitializeComponent();

            App.Register(this);
        }

        public void Tick()
        {
            RightEngine.Height = ((FrameworkElement)RightEngine.Parent).ActualHeight * SystemManager.Instance.Computer.Thrust;
            LeftEngine.Height = ((FrameworkElement)LeftEngine.Parent).ActualHeight * SystemManager.Instance.Computer.Thrust;
        }
    }
}
