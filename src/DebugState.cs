using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GTAPilot
{
    class DebugState
    {
        List<object> _images = new List<object>();

        public void Add(object img)
        {
            if (img is Mat)
            {
                img = ((Mat)img).ToImage<Gray, byte>();
            }

            _images.Add(img);
        }

        public object[] Get(int count)
        {
            var ret = new List<object>(_images.Take(count));

            for (var i = 0; i < count - ret.Count; i++) ret.Add(null);

            return ret.ToArray();
        }

        public void SetError(string msg)
        {
            // Trace.WriteLine(msg);
        }
    }
}
