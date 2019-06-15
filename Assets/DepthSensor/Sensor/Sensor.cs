using System;
using UnityEngine;

namespace DepthSensor.Sensor {
    public class Sensor<T> {
        public readonly int width;
        public readonly int height;
        public readonly T[] data;
        public event Action<Sensor<T>> OnNewFrame;
        public bool Active {
            get { return _active; }
            set { if (_active != value) {
                    _active = value;
                    _onActiveChanged(this);
                }}
        }

        private Action<Sensor<T>> _onActiveChanged;
        private bool _active;

        protected internal Sensor(int width, int height, T[] data = null) {
            this.width = width;
            this.height = height;
            this.data = data ?? new T[width * height];
            _onActiveChanged = sensor => {throw new NotImplementedException();};
        }

        public class Internal {
            private readonly Sensor<T> _sensor;

            protected internal Internal(Sensor<T> sensor) {
                _sensor = sensor;
            }
            
            protected internal void NewFrame(T[] newData) {
                Set(newData);
                OnNewFrame();
            }
            
            protected internal virtual void Set(T[] newData) {
                Array.Copy(newData, _sensor.data, newData.Length);
            }
            
            protected internal void OnNewFrame() {
                if (_sensor.OnNewFrame != null) _sensor.OnNewFrame(_sensor);
            }

            protected internal void SetOnActiveChanged(Action<Sensor<T>> onActiveChanged = null) {
                _sensor._onActiveChanged = onActiveChanged;
            }
        }
    }
}