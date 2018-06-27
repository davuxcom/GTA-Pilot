using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace GTAPilot
{
    class ReplayFrameProducer : IFrameProducer, INotifyPropertyChanged
    {
        public event Action<int, Bitmap> FrameProduced;
        public event PropertyChangedEventHandler PropertyChanged;

        public int FrameCount => _frames.Length;
        public int CurrentFrame => _currentId;
        public bool IsPaused => _isPaused;

        string[] _frames;
        int _currentId = 0;
        bool _isPaused;

        public ReplayFrameProducer(string dir)
        {
            _frames = Directory.GetFiles(dir);
        }

        public void Begin()
        {
            var t = new Thread(() =>
            {
                while (true)
                {
                    if (_isPaused)
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    FrameProduced(_currentId, new Bitmap(_frames[_currentId++]));

                    if (_currentId >= _frames.Length) _currentId = 0;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFrame)));
                }
            });
            t.IsBackground = true;
            t.Priority = ThreadPriority.Highest;
            t.Start();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        internal void PlayPause()
        {
            _isPaused = !_isPaused;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPaused)));
        }

        internal void Seek(int value)
        {
            _currentId = value;
        }
    }
}
