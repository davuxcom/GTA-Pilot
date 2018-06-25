﻿using GTAPilot.Extensions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GTAPilot
{
    public partial class PrimaryFlightDisplayControl : UserControl
    {
        Polygon _top = new Polygon();
        Polygon _bottom = new Polygon();
        DispatcherTimer _fpsTimer = new DispatcherTimer();

        List<Line> _pitchLines = new List<Line>();


        public PrimaryFlightDisplayControl()
        {
            InitializeComponent();

            _top.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0480D8"));
            _bottom.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#744C07"));

            PFDBackground.Children.Add(_top);
            PFDBackground.Children.Add(_bottom);

            for (var i = 0; i < 40; i++)
            {
                var line = new Line { Stroke = Brushes.White, StrokeThickness = 2 };

                _pitchLines.Add(line);
                PFDBackground.Children.Add(line);
            }

            TransformGroup group = new TransformGroup();
            group.Children.Add(new TranslateTransform());
            group.Children.Add(new RotateTransform());

            PFDBackground.RenderTransform = group;
            PFDBackground.RenderTransformOrigin = new Point(0.5, 0.5);

            _fpsTimer.Interval = TimeSpan.FromMilliseconds(App.FPS);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();

            SizeChanged += PrimaryFlightDisplayControl_SizeChanged;
        }

        private void PrimaryFlightDisplayControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _top.Points.Clear();

            _top.Points.Add(new Point(-200, -200));
            _top.Points.Add(new Point(PFDBackground.ActualWidth + 200, -200));
            _top.Points.Add(new Point(PFDBackground.ActualWidth + 200, PFDBackground.ActualHeight / 2));
            _top.Points.Add(new Point(-200, PFDBackground.ActualHeight / 2));

            _bottom.Points.Clear();
            _bottom.Points.Add(new Point(-200, PFDBackground.ActualHeight / 2));
            _bottom.Points.Add(new Point(PFDBackground.ActualWidth + 200, PFDBackground.ActualHeight / 2));
            _bottom.Points.Add(new Point(PFDBackground.ActualWidth + 200, PFDBackground.ActualHeight + 200));
            _bottom.Points.Add(new Point(-200, PFDBackground.ActualHeight + 200));

            var start = 0;
            var step = 500 / 6;
            for (var i = 0; i < 25; i += 4)
            {
                var l = _pitchLines[i];
                l.X1 = 200;
                l.X2 = 300;
                l.Y1 = start;
                l.Y2 = start;
                var l2 = _pitchLines[i + 1];
                l2.X1 = 230;
                l2.X2 = 270;
                l2.Y1 = start + (step / 4) * 1;
                l2.Y2 = start + (step / 4) * 1;
                var l3 = _pitchLines[i + 2];
                l3.X1 = 220;
                l3.X2 = 280;
                l3.Y1 = start + (step / 4) * 2;
                l3.Y2 = start + (step / 4) * 2;
                var l4 = _pitchLines[i + 3];
                l4.X1 = 230;
                l4.X2 = 270;
                l4.Y1 = start + (step / 4) * 3;
                l4.Y2 = start + (step / 4) * 3;
                start += step;
            }

        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            LayoutBackground();

            AltitudeText.Text = "" + Math.Round(Timeline.Altitude);
            SpeedText.Text = "" + Math.Round(Timeline.Speed);
        }

        private void LayoutBackground()
        {
            var rt = PFDBackground.Get<RotateTransform>();
            var tt = PFDBackground.Get<TranslateTransform>();

            var pitch_mult = 1.5;
            var roll_mult = -1;

            tt.Y = Timeline.PitchAvg * pitch_mult;

            var roll = Timeline.RollAvg;
            if (roll < 0)
            {
                rt.Angle = (360 + roll) * roll_mult;
            }
            else
            {
                rt.Angle = roll * roll_mult;
            }
        }
    }
}