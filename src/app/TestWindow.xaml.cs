using Emgu.CV;
using Emgu.CV.Structure;
using GTAPilot.Extensions;
using GTAPilot.Indicators_v2;
using System.Drawing;
using System.IO;
using System.Windows;

namespace GTAPilot
{
    public partial class TestWindow : Window
    {
        string[] _files = Directory.GetFiles(@"c:\save\recording1");

        ISimpleIndicator _indicator;

        public TestWindow()
        {
            _indicator = new AirspeedIndicator_v2();

            InitializeComponent();

            FrameSlider.Maximum = _files.Length - 1;
            FrameSlider.Value = 0;
            FrameSlider.ValueChanged += FrameSlider_ValueChanged;

            FrameSlider_ValueChanged(null, null);
        }

        private void FrameSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var inFrame = new Bitmap(_files[(int)FrameSlider.Value]);
            img1.Source = inFrame.ToImageSource();

            var frame = new Image<Bgr, byte>(inFrame);

            object[] debugState = null;
            var v = _indicator.ReadValue(frame, ref debugState);

            dynamic debugImg1 = (debugState != null ? debugState[0] : null);

            img2.Source = ((Bitmap)debugImg1?.ToBitmap()).ToImageSource();
            val.Text = v.ToString();
        }
    }
}
