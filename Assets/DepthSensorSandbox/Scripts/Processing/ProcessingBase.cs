using System;
using DepthSensor.Buffer;

namespace DepthSensorSandbox.Processing {
    public abstract class ProcessingBase : IDisposable {
        public bool OnlyRawBufferIsInput = true;
        public bool Active = true;
        
        protected DepthBuffer _inDepth;
        protected DepthBuffer _rawBuffer;
        protected DepthBuffer _out;
        protected DepthBuffer _prev;
        protected Sampler _s = new Sampler();

        public void Process(DepthBuffer rawBuffer, DepthBuffer outBuffer, DepthBuffer prevBuffer) {
            if (Active) {
                _rawBuffer = rawBuffer;
                _out = outBuffer;
                _prev = prevBuffer;
                _inDepth = OnlyRawBufferIsInput ? _rawBuffer : _out;
                _s.SetDimens(_rawBuffer.width, _rawBuffer.height);
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
        
        protected bool CheckValid(AbstractBuffer2D b) {
            return b != null && b.width == _out.width && b.height == _out.height;
        }

        public virtual void Dispose() {}
    }
}