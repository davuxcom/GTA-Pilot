using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot.Indicators_v2
{
    interface ISimpleIndicator
    {
        double ReadValue(Image<Bgr, byte> frame, ref object[] debugState);
    }
}
