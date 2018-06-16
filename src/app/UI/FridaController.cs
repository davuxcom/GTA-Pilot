using System;
using System.ComponentModel;

namespace GTAPilot
{
    internal class FridaController : INotifyPropertyChanged
    {
        public event Action<string> OnMessage;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsConnected => false; // TODO

        public FridaController()
        {
            // TODO connect
        }

        public void SendMessage(string message)
        {
            // TODO decide sync or async
        }
    }
}