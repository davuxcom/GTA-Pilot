using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;

namespace GTAPilot
{
    class ReplayFrameProducer : IFrameProducer, INotifyPropertyChanged
    {
        public event Action<int, Bitmap> FrameProduced;
        public event PropertyChangedEventHandler PropertyChanged;

        public int FrameCount => _framePaths.Length;
        public int CurrentFrame => _currentId;
        public bool IsPaused => _isPaused;

        string[] _framePaths;
        Bitmap[] _frames;
        int _currentId = 0;
        bool _isPaused;

        public ReplayFrameProducer(string dir)
        {
            _framePaths = Directory.GetFiles(dir);
            _frames = new Bitmap[_framePaths.Length];
        }

        public void Begin()
        {
            var t = new Thread(() =>
            {
                int loadId = 1;
                while (true)
                {
                    _frames[loadId] = new Bitmap(_framePaths[loadId+=2]);

                    if (loadId+2 >= _framePaths.Length) return;
                }
            });
            t.IsBackground = true;
            t.Priority = ThreadPriority.Highest;
            t.Start();

            t = new Thread(() =>
            {
                int loadId = 0;
                while (true)
                {
                    _frames[loadId] = new Bitmap(_framePaths[loadId+=2]);

                    if (loadId+2 >= _framePaths.Length) return;
                }
            });
            t.IsBackground = true;
            t.Priority = ThreadPriority.Highest;
            t.Start();

            t = new Thread(() =>
            {
                while (true)
                {
                    if (_isPaused)
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    while (_frames[_currentId] == null) Thread.Sleep(1);

                    FrameProduced(_currentId, _frames[_currentId]);

                    _frames[_currentId] = null;

                    _currentId++;

                    if (_currentId >= _framePaths.Length) _currentId = 0;

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
         //   _currentId = value;
        }
    }
}
