using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace RTLighting.Utilities
{
    class Controller
    {
        public Vector2 LeftStick { get; private set; }

        private Vector2 _targetLeftStick;

        private Dictionary<Key, bool> _currentState;
        private Dictionary<Key, bool> _lastState;

        private static object KeyLock = new object();

        public Controller()
        {
            LeftStick = Vector2.Zero;
            _targetLeftStick = Vector2.Zero;

            _currentState = new Dictionary<Key, bool>();
            _lastState = new Dictionary<Key, bool>();

            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (!_currentState.ContainsKey(key))
                {
                    _currentState.Add(key, false);
                    _lastState.Add(key, false);   
                }
            }
        }

        public bool KeyPressed(Key key)
        {
            return _currentState[key];
        }

        public void UpdateKey(Key key, bool value)
        {
            lock (KeyLock)
            {
                _currentState[key] = value;   
            }
        }

        public void Update(GameTime gameTime)
        {
            _targetLeftStick.X = 0;
            _targetLeftStick.Y = 0;

            if (KeyPressed(Key.Left))
            {
                _targetLeftStick.X = -1;
            }
            else if (KeyPressed(Key.Right))
            {
                _targetLeftStick.X = 1;
            }

            if (KeyPressed(Key.Up))
            {
                _targetLeftStick.Y = -1;
            }
            else if (KeyPressed(Key.Down))
            {
                _targetLeftStick.Y = 1;
            }

            if (_targetLeftStick.LengthSquared() > 0)
            {
                _targetLeftStick.Normalize();
            }

            LeftStick = Vector2.Lerp(LeftStick, _targetLeftStick, 10 * gameTime.ElapsedTime);

            lock (KeyLock)
            {
                foreach (Key key in _currentState.Keys)
                {
                    _lastState[key] = _currentState[key];
                }
            }
        }
    }
}
