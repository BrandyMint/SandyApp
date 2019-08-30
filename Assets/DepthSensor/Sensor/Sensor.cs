using System;
using DepthSensor.Buffer;
using UnityEngine;
using UnityEngine.Assertions;

namespace DepthSensor.Sensor {
    public class Sensor<T> : AbstractSensor where T : AbstractBuffer {
        protected T[] _buffers;
        protected int _first;
        
        private T[] _buffersCacheForExternalUse;

        public Sensor(T buffer) {
            _buffersCount = 1;
            _buffers = new[] {buffer};
        }

        public Sensor(bool available) : this(null) {
            Available = available;
        }

        protected override void OnBuffersCountChanged(int newCount) {
            Assert.IsTrue(newCount > 0);
            var allCount = Math.Max(_buffers.Length, newCount);
            var newBuffers = new T[allCount];
            var newFirst = newCount - 1;
            
            for (int i = 0; i < allCount; ++i) {
                var newI = GetIdx(i, newFirst, allCount);
                if (i < BuffersCount) {
                    newBuffers[newI] = Get(i);
                } else {
                    newBuffers[newI] = GetNewest().CreateSome<T>();
                }
            }

            _buffers = newBuffers;
            _first = newFirst;
            BuffersValid = Math.Min(newCount, BuffersValid);
        }

        public T Get(int i) {
            return _buffers[GetIdx(i, _first, BuffersCount)];
        }

        public T GetNewest() {
            return Get(0);
        }

        public T GetOldest() {
            return Get(BuffersCount - 1);
        }

        public T GetNewestAndLock() {
            var buffer = GetNewest();
            buffer.Lock();
            return buffer;
        }

        public T[] GetFreeBuffersAndLock() {
            var len = Mathf.Min(BuffersValid, BuffersCount - 1);
            if (_buffersCacheForExternalUse == null || _buffersCacheForExternalUse.Length != len)
                _buffersCacheForExternalUse = new T[len];
            for (int i = 0; i < len; ++i) {
                var b = Get(i);
                b.Lock();
                _buffersCacheForExternalUse[i] = b;
            }

            return _buffersCacheForExternalUse;
        }

        private static int GetIdx(int i, int first, int len) {
            return (len + first - i) % len;
        }

        public override void Dispose() {
            foreach (var buf in _buffers) {
                buf?.Dispose();
            }
            _buffers = null;
        }

        public class Internal : AbstractSensor.Internal {
            private readonly Sensor<T> _sensor;

            protected internal Internal(Sensor<T> sensor) : base(sensor) {
                _sensor = sensor;
            }

            protected internal override void OnNewFrameBackground() {
                var len = _sensor.BuffersCount;
                _sensor._first = (len + _sensor._first + 1) % len;
                base.OnNewFrameBackground();
            }
        }
    }
}