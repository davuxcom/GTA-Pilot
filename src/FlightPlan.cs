using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace GTAPilot
{
    class FlightPlan : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int CurrentIndex { get; private set; }
        public PointF Target => Points[CurrentIndex];
        public LineSegment2DF TargetLine => new LineSegment2DF(Points[CurrentIndex - 1], Points[CurrentIndex]);
        public double TargetHeading => Math2.GetPolarHeadingFromLine(Target, Timeline.CurrentLocation);

        public ObservableCollection<PointF> Points { get; }

        public FlightPlan()
        {
            Points = new ObservableCollection<PointF>();
        }

        public void Load(IEnumerable<PointF> collection)
        {
            CurrentIndex = 0;

            Points.Clear();

            foreach (var p in collection) Points.Add(p);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentIndex)));
        }

        public void LoadFromFile(string fileName)
        {
            CurrentIndex = 0;
            Points.Clear();

            foreach (var line in System.IO.File.ReadAllLines(fileName))
            {
                var parts = line.Split(',');
                Debug.Assert(parts.Length == 2);
                Points.Add(new PointF((float)double.Parse(parts[0]), (float)double.Parse(parts[1])));
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentIndex)));
        }

        internal bool UpdateLocation()
        {
            if (Points.Count == 0) return false;

            var dist = Math2.GetDistance(Points[CurrentIndex], Timeline.CurrentLocation);
            bool isCloseToPoint = dist < 60;
            if (isCloseToPoint)
            {
                CurrentIndex++;
                Trace.WriteLine($"Flight Plan: Advance: {CurrentIndex}");

                if (CurrentIndex > Points.Count - 1)
                {
                    // Loop at end, if possible.
                    CurrentIndex = 0;
                }

                App.Current.Dispatcher.BeginInvoke((Action) (() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentIndex)));
                }));
                return true;
            }

            return false;
        }
    }
}
