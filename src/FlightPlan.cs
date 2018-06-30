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

        public Runway Source { get; private set; }
        public Runway Destination { get; private set; }
        public int CurrentIndex { get; private set; }
        public ObservableCollection<PointF> Points { get; }
        public LineSegment2DF TargetLine => new LineSegment2DF(Points[CurrentIndex - 1], Points[CurrentIndex]);
        public double TargetHeading => Math2.GetPolarHeadingFromLine(Points[CurrentIndex], Timeline.CurrentLocation);

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

            // Departure
            Source = Runways.LSI_RW03;
            Points.Add(Runways.LSI_RW03.StartPoint);
            Points.Add(Runways.LSI_RW03.EndPoint);
            Points.Add(Runways.LSI_RW03.ExtendForward(100));

            foreach (var line in System.IO.File.ReadAllLines(fileName))
            {
                var parts = line.Split(',');
                Debug.Assert(parts.Length == 2);
                Points.Add(new PointF((float)double.Parse(parts[0]), (float)double.Parse(parts[1])));
            }

            // Approach
            Points.Add(Runways.LSI_RW30R.ExtendBackward(800));
            Points.Add(Runways.LSI_RW30R.StartPoint);
            Points.Add(Runways.LSI_RW30R.EndPoint);
            Destination = Runways.LSI_RW30R;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentIndex)));
        }

        internal bool UpdateLocation()
        {
            if (Points.Count == 0) return false;

            var dist = Math2.GetDistance(Points[CurrentIndex], Timeline.CurrentLocation);

            double dist_max = 40;

            if (CurrentIndex > 0 && CurrentIndex + 1 < Points.Count - 1)
            {
                var nextLine = Math2.GetPolarHeadingFromLine(Points[CurrentIndex], Points[CurrentIndex + 1]);
                var angle_delta = Math.Abs(Math2.DiffAngles(Math2.GetPolarHeadingFromLine(TargetLine), nextLine));
                dist_max += angle_delta * 1.5;
            }

            bool isCloseToPoint = dist < dist_max;
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
