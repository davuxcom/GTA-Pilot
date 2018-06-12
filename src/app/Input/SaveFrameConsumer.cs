using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace GTAPilot
{
    class SaveFrameConsumer
    {
        List<Bitmap> _frames = new List<Bitmap>();
        string _dir;

        public SaveFrameConsumer(string dir)
        {
            _dir = dir;

            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        internal void HandleFrameArrived(FrameData data)
        {
            _frames.Add(data.Frame);
        }

        internal void SaveAll()
        {
            for(var i = 0; i < _frames.Count; i++)
            {
                var f = _frames[i];
                f.Save($"{_dir}\\{(i.ToString().PadLeft(4, '0'))}.bmp");
            }
        }
    }
}
