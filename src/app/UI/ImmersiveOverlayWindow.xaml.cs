﻿using System.Linq;
using System.Text;
using System.Windows;

namespace GTAPilot
{
    public partial class ImmersiveOverlayWindow : Window
    {
        public RelayCommand FlightPlanOpen { get; }
        public RelayCommand FlightPlanSave { get; }
        public RelayCommand FlightPlanEdit { get; }
        public RelayCommand Analyze { get; }

        private MainWindow _analyze = new MainWindow();

        public ImmersiveOverlayWindow()
        {
            InitializeComponent();

            var plan = SystemManager.Instance.FlightPlan;
            FlightPlanEdit = new RelayCommand(() =>
            {
                var editor = new FlightPlanBuidler(plan.Points.ToArray());
                editor.ShowDialog();

                plan.Load(editor.Points);
            });

            FlightPlanSave = new RelayCommand(() =>
            {
                StringBuilder ret = new StringBuilder();
                foreach (var p in plan.Points)
                {
                    ret.AppendLine($"{p.X},{p.Y}");
                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "FlightPlan";
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";

                if (dlg.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dlg.FileName, ret.ToString());
                }
            });

            FlightPlanOpen = new RelayCommand(() =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = "FlightPlan";
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";

                if (dlg.ShowDialog() == true)
                {
                    SystemManager.Instance.FlightPlan.LoadFromFile(dlg.FileName);
                }
            });

            Analyze = new RelayCommand(() =>
            {
                _analyze.Show();
            });

            DataContext = this;
        }
    }
}
