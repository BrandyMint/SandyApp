using System;
using System.Linq;
using System.Threading;
using UnityEngine.Assertions;

namespace DepthSensor.Buffer {
    public abstract class AbstractBuffer : IBuffer {
        public readonly object SyncRoot = new object();

        public T CreateSome<T>() where T : IBuffer {
            return Create<T>(GetArgsForCreateSome());
        }
        
        public static T Create<T>(object[] args) where T : IBuffer {
            var type = typeof(T);
            return (T) Create(type, args);
        }
        
        public static object Create(Type type, object[] args) {
            Assert.IsTrue(typeof(IBuffer).IsAssignableFrom(type));
            var constructor = type.GetConstructor(args.Select(a => a.GetType()).ToArray());
            Assert.IsNotNull(constructor, $"Cant find constructor for {type.Name}");
            return constructor.Invoke(args);
        }

        public abstract T Copy<T>() where T : IBuffer;
        
        public abstract void Clear();

        protected internal abstract object[] GetArgsForCreateSome();

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