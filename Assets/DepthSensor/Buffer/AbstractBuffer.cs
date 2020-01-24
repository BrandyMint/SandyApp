using System;
using System.Linq;
using System.Threading;
using UnityEngine.Assertions;

namespace DepthSensor.Buffer {
    public abstract class AbstractBuffer : IBuffer {
        public readonly object SyncRoot = new object();

        public T CreateSome<T>() where T : IBuffer {
            var type = GetType();
            var args = GetArgsForCreateSome();
            var constructor = type.GetConstructor(args.Select(a => a.GetType()).ToArray());
            Assert.IsNotNull(constructor, $"Cant find constructor for {type.Name}");
            return (T) constructor.Invoke(args);
        }

        public abstract T Copy<T>() where T : IBuffer;
        
        public abstract void Clear();

        public abstract long LengthInBytes();

        protected internal abstract object[] GetArgsForCreateSome();

        protected internal abstract void Set(byte[] newData);

        protected internal abstract void Set(IntPtr newData);

        protected internal abstract void SetBytes(IntPtr newData, long copyBytes);

        public virtual void Dispose() {
            if (Lock(100))
                Unlock();
        }

        public bool Lock(int milliseconds = -1) {
            if (milliseconds < 0) {
                Monitor.Enter(SyncRoot);
                return true;
            }
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