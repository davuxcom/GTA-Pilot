using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace GTAPilot
{
    class FlightPlan
    {
        public int CurrentIndex { get; private set; }
        public PointF Target => _points[CurrentIndex];
        public double TargetHeading => Math2.GetPolarHeadingFromLine(Timeline.CurrentLocation, Target);

        List<PointF> _points = new List<PointF>();

        public FlightPlan(string fileName)
        {
            var lines = System.IO.File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                Debug.Assert(parts.Length == 2);
                _points.Add(new PointF((float)double.Parse(parts[0]), (float)double.Parse(parts[1])));
            }
        }

        internal void UpdateLocation()
        {
            bool isCloseToPoint = Math2.GetDistance(_points[CurrentIndex], Timeline.CurrentLocation) < 4;
            if (isCloseToPoint)
            {
                CurrentIndex++;

                if (CurrentIndex > _points.Count - 1)
                {
                    // Loop at end, if possible.
                    CurrentIndex = 0;
                }
            }
        }
    }
}
