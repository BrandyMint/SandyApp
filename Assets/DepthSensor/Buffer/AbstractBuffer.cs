using System;
using System.Threading;

namespace DepthSensor.Buffer {
    public abstract class AbstractBuffer : IBuffer {
        public readonly object SyncRoot = new object();
        
        public abstract AbstractBuffer CreateSome();

        protected internal abstract void Set(IntPtr newData);

        protected internal abstract void SetBytes(IntPtr newData, long copyBytes);

        public virtual void Dispose() {
            if (Lock(100))
                Unlock();
        }

        public bool Lock(int milliseconds = -1) {
            return Monitor.TryEnter(SyncRoot, milliseconds);
        }

        public void Unlock() {
            Monitor.Exit(SyncRoot);
        }

        public void SafeUnlock() {
            if (Monitor.IsEntered(SyncRoot))
                Unlock();
        }
    }
}