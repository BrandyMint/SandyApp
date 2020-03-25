using System;
using DepthSensor.Buffer;
using DepthSensor.Device;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public abstract class ProcessingBase : IDisposable {
        public bool OnlyRawBufferIsInput = true;
        public bool Active {
            get => _active; 
            set{ 
                if (value != _active)
                    OnActiveChange(value);
                _active = value;
            }
        }

        protected DepthBuffer _inDepth;
        protected DepthBuffer _rawBuffer;
        protected DepthBuffer _out;
        protected DepthBuffer _prev;
        protected Sampler _s = Sampler.Create();
        protected bool _active = true;
        protected DepthBuffer _errorsMap;

        public bool Process(DepthBuffer rawBuffer, DepthBuffer outBuffer, DepthBuffer prevBuffer) {
            if (_active) {
                PrepareProcessing(rawBuffer, outBuffer, prevBuffer);
                ProcessInternal();
                return true;
            }
            return false;
        }

        protected void PrepareProcessing(DepthBuffer rawBuffer, DepthBuffer outBuffer, DepthBuffer prevBuffer) {
            _rawBuffer = rawBuffer;
            _out = outBuffer;
            _prev = prevBuffer;
            _inDepth = OnlyRawBufferIsInput ? _rawBuffer : _out;
        }

        protected abstract void ProcessInternal();

        public void InitInMainThread(DepthSensorDevice device) {
            var buffer = device.Depth.GetNewest();
            _s.SetDimens(buffer.width, buffer.height);
            InitInMainThreadInternal(device);
        }

        public void SetErrorsMap(DepthBuffer errorsMap) {
            _errorsMap = errorsMap;
        }

        protected virtual void InitInMainThreadInternal(DepthSensorDevice device) {}
        protected virtual void OnActiveChange(bool active) {}

        protected static bool ReCreateIfNeed<T>(ref T[] a, int len) {
            if (a == null || a.Length != len) {
                a = new T[len];
                return true;
            }
            return false;
        }
        
        protected bool CheckValid(AbstractBuffer2D b) {
            return b != null && b.width == _out.width && b.height == _out.height;
        }

        public virtual void SetCropping(Rect cropping01) {
            _s.SetCropping01(cropping01);
        }

        public Sampler GetSampler() {
            return _s;
        }

        public virtual void Dispose() {}
    }
}