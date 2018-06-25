using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot.Indicators
{
    interface ISimpleIndicator
    {
        double CachedTuningValue { get; }
        double LastGoodValue { get; }

        double ReadValue(IndicatorData data, ref object[] debugState);
    }
}
