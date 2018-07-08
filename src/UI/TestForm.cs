using Emgu.CV;
using Emgu.CV.Structure;
using GTAPilot;
using GTAPilot.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAFlightDeck
{
    public partial class TestForm : Form
    {
        public static Emgu.CV.Structure.Hsv Low = new Emgu.CV.Structure.Hsv(0, 0, 0);
        public static Emgu.CV.Structure.Hsv High = new Emgu.CV.Structure.Hsv(180, 255, 255);

        DesktopDuplication.DesktopDuplicator desktop;

        public TestForm()
        {
            InitializeComponent();

            desktop = new DesktopDuplication.DesktopDuplicator(XboxApp.GetWindow());


            Timer t = new Timer();
            t.Interval = 200;
            t.Tick += T_Tick;
            t.Start();
        }

        private void T_Tick(object sender, EventArgs e)
        {

            var f = desktop.GetLatestFrame();
            if (f == null)
            {
                return;
            }

            ProcessImageFrame(f);

        }

        private void ProcessImageFrame(Bitmap desktopImage)
        {
            var menu = new MenuReader();

            var ds = new DebugState();
            menu.HandleFrameArrived(new Image<Bgr,byte>(desktopImage), ds);

            Trace.WriteLine($"Location: {menu.Location}");

            img.Image = (IImage)ds.Get(10)[1];

            /*
            var hsv = image.Convert<Hsv, byte>();

            img.Image = hsv.InRange(Low, High);
            */
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Low.Hue = double.Parse(textBox1.Text);
            }
            catch (Exception) { }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {

                Low.Satuation = double.Parse(textBox2.Text);
            }
            catch (Exception)
            { }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {

                Low.Value = double.Parse(textBox3.Text);
            }
            catch (Exception)
            { }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            try
            {

                High.Hue = double.Parse(textBox4.Text);
            }
            catch (Exception)
            { }
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {

                High.Satuation = double.Parse(textBox5.Text);
            }
            catch (Exception)
            { }
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            try
            {
                High.Value = double.Parse(textBox6.Text);
            }
            catch (Exception)
            { }
        }
    }
}
