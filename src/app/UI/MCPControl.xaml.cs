using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace GTAPilot
{
    public partial class MCPControl : UserControl
    {
        public ModeControlPanel MCP { get; }

        public MCPControl()
        {
            InitializeComponent();

            MCP = SystemManager.Instance.MCP;
            DataContext = this;
        }


        private void TextBox_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var txt = (TextBox)sender;
            bool isPID = (string)txt.Tag == "PID";

            double next = 0;
            if (isPID)
            {
                var vl = double.Parse(txt.Text);

                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    next = (vl + Math.Sign(e.Delta) * 0.01);
                }
                else
                {
                    next = (vl + Math.Sign(e.Delta) * 0.1);
                }
            }
            else
            {
                var iTxt = int.Parse(txt.Text);
                bool isHDG = (string)txt.Tag == "HDG";
                bool isALT = (string)txt.Tag == "ALT";

                var v = isALT ? 100 : 1;

                next = (iTxt + Math.Sign(e.Delta) * v);

                if (isALT)
                {
                    next = next / 100;
                    next = Math.Round(next);
                    next = next * 100;
                }

                if (isHDG)
                {
                    if (next > 359) next = 0;
                    if (next < 0) next = 359;
                }


            }
            txt.Text = next.ToString();
        }
    }
}
