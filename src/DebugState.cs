using System.Collections.Generic;

namespace GTAPilot
{
    class DebugState
    {
        List<object> _images = new List<object>();

        public void Add(object img)
        {
            _images.Add(img);
        }

        public object[] Get(int count)
        {
            var ret = new List<object>(_images);

            for (var i = 0; i < count - ret.Count; i++) ret.Add(null);

            return ret.ToArray();
        }
    }
}
