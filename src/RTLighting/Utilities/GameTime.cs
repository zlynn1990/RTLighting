using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace RTLighting.Utilities
{
    class GameTime
    {
        public int CurrentFps { get; private set; }

        public int TargetFps { get; private set; }

        public float ElapsedTime { get; private set; }

        private Dictionary<string, Stopwatch> _timers;

        private int _targetFrameLength;

        private int[] _fpsBuffer;
        private int _bufferIndex;

        private readonly Font _font;
        private readonly SolidBrush _brush;

        public GameTime()
        {
            TargetFps = Display.GetMonitorRefreshRate();

            _targetFrameLength = 1000 / TargetFps;

            _timers = new Dictionary<string, Stopwatch>();

            _fpsBuffer = new int[60];

            _font = new Font("Arial", 16);
            _brush = new SolidBrush(Color.White);
        }

        public void StartEvent(string eventName)
        {
            if (!_timers.ContainsKey(eventName))
            {
                _timers.Add(eventName, new Stopwatch());
            }

            _timers[eventName].Reset();
            _timers[eventName].Start();
        }

        public void EndEvent(string eventName)
        {
            if (!_timers.ContainsKey(eventName))
            {
                _timers.Add(eventName, new Stopwatch());
            }

            _timers[eventName].Stop();
        }

        public void BeginUpdate()
        {
            StartEvent("Frame");
        }

        public void FinalizeUpdate()
        {
            EndEvent("Frame");

            int elapsedTime = Math.Max((int)_timers["Frame"].ElapsedMilliseconds, 1);

            ElapsedTime = elapsedTime * 0.001f;

            _fpsBuffer[_bufferIndex] = 1000 / elapsedTime;

            if (_bufferIndex + 1 == _fpsBuffer.Length)
            {
                _bufferIndex = 0;
            }
            else
            {
                _bufferIndex++;
            }

            int fpsSum = 0;

            for (int i=0; i < _fpsBuffer.Length; i++)
            {
                fpsSum += _fpsBuffer[i];
            }

            CurrentFps = Math.Min(fpsSum / _fpsBuffer.Length, TargetFps);

            int sleepTime = _targetFrameLength - elapsedTime;

            if (sleepTime > 0)
            {
                Thread.Sleep(sleepTime);
            }
        }

        public void Draw(Graphics graphics, int screenWidth, int screenHeight)
        {
            graphics.DrawString(CurrentFps + " Fps", _font, _brush, 20, screenHeight - 80);
        }
    }
}
