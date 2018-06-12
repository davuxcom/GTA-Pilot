using System;
using System.Collections.Generic;
using System.Linq;

namespace GTAPilot
{
    public class FpsCounter
    {
        public DateTime LastFrameTime = DateTime.Now;
        public int Fps;

        private List<DateTime> Frames = new List<DateTime>();

        public int CalculateFps()
        {
            var oneSecondAgo = DateTime.Now.AddSeconds(-1);

            var toRemove = Frames.ToArray().Where(f => f < oneSecondAgo);
            try
            {
                foreach (var f in toRemove) Frames.Remove(f);
            }
            catch (Exception) { }

            Fps = Frames.Count;
            return Fps;
        }

        public void GotFrame()
        {
            var frameTime = DateTime.Now;
            LastFrameTime = frameTime;
            Frames.Add(frameTime);

            CalculateFps();
        }

        public int LastFrameMsAgo
        {
            get
            {
                return (int)Math.Round((DateTime.Now - LastFrameTime).TotalMilliseconds);
            }
        }
    }
}
