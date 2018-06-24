using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace GTAPilot
{
    class FlightPlan
    {
        public int CurrentIndex { get; private set; }
        public PointF Target => Points[CurrentIndex];
        public double TargetHeading => Math2.GetPolarHeadingFromLine(Target, Timeline.CurrentLocation);

        public List<PointF> Points = new List<PointF>();

        public FlightPlan(string fileName)
        {
            var lines = System.IO.File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                Debug.Assert(parts.Length == 2);
                Points.Add(new PointF((float)double.Parse(parts[0]) * FlightPlanBuidler.FlightPlanScaleFactor, 
                    (float)double.Parse(parts[1]) * FlightPlanBuidler.FlightPlanScaleFactor));
            }
        }


        int ticks = 0;
        internal void UpdateLocation()
        {
            var dist = Math2.GetDistance(Points[CurrentIndex], Timeline.CurrentLocation);
            bool isCloseToPoint = dist < 120;

            ticks++;
            if (ticks % 50 == 0)
            {
              //  Trace.WriteLine($"Flight Plan: {dist} {isCloseToPoint}");
            }

            if (isCloseToPoint)
            {
                CurrentIndex++;
                Trace.WriteLine($"Flight Plan: Advance: {CurrentIndex}");

                if (CurrentIndex > Points.Count - 1)
                {
                    // Loop at end, if possible.
                    CurrentIndex = 0;
                }
            }
        }
    }
}
