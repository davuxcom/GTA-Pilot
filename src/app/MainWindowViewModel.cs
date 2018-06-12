using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAPilot
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int CaptureEnqueueFPS => _inputManager.GetFpsCounter(FpsCounterType.Capture_Enqueue);
        public int CaptureDequeueFPS => _inputManager.GetFpsCounter(FpsCounterType.Capture_Dequeue);


        InputManager _inputManager;


        public MainWindowViewModel(InputManager inputManager)
        {
            _inputManager = inputManager;
        }

        public void Tick()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CaptureEnqueueFPS)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CaptureDequeueFPS)));
        }
    }
}
