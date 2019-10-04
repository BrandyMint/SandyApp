using System;
using DepthSensor.Buffer;

namespace DepthSensorSandbox.Processing {
    public abstract class ProcessingBase : IDisposable {
        public bool OnlyRawBuffersIsInput = true;
        public bool Active = true;
        
        protected DepthBuffer[] _rawBuffers;
        protected DepthBuffer _inOut;
        protected DepthBuffer _inDepth;
        protected Sampler _s = new Sampler();

        public void Process(DepthBuffer[] rawBuffers, DepthBuffer inOut) {
            if (Active) {
                _rawBuffers = rawBuffers;
                _inOut = inOut;
                _inDepth = OnlyRawBuffersIsInput ? _rawBuffers[0] : _inOut;
                _s.SetDimens(_inDepth.width, _inDepth.height);
                ProcessInternal();
            }
        }

        protected abstract void ProcessInternal();

        public void InitInMainThread(DepthBuffer buffer) {
            if (Active) {
                InitInMainThreadInternal(buffer);
            }
        }

        protected virtual void InitInMainThreadInternal(DepthBuffer buffer) {}

        protected static bool ReCreateIfNeed<T>(ref T[] a, int len) {
            if (a == null || a.Length != len) {
                a = new T[len];
                return true;
            }
            return false;
        }
        
        protected bool CheckValid(Buffer2D b) {
            return b != null && b.width == _inOut.width && b.height == _inOut.height;
        }

        public virtual void Dispose() {}
    }
}