using System;
using UnityEngine;

namespace DepthSensor.Sensor {
    public class Sensor<T> : AbstractSensor  {
        public readonly int width;
        public readonly int height;
        public readonly T[] data;
        public event Action<Sensor<T>> OnNewFrame;
        public event Action<Sensor<T>> OnNewFrameBackground;

        protected internal Sensor(int width, int height, T[] data = null) {
            this.width = width;
            this.height = height;
            this.data = data ?? new T[width * height];
            _onActiveChanged = sensor => {throw new NotImplementedException();};
        }

        protected internal Sensor(bool available) : this(0, 0) {
            Available = available;
        }

        public Vector2 GetXYFrom(long i) {
            return new Vector2(
                i % width,
                i / width
            );
        }
        
        public Vector2 GetXYFrom(int i) {
            return new Vector2(
                i % width,
                i / width
            );
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
            
            protected internal void OnNewFrameBackground() {
                if (_sensor.OnNewFrameBackground != null) _sensor.OnNewFrameBackground(_sensor);
            }

            protected internal void SetOnActiveChanged(Action<AbstractSensor> onActiveChanged = null) {
                _sensor._onActiveChanged = onActiveChanged;
            }
        }
    }
}