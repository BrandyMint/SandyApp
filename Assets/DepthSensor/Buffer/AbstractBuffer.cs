using System;
using System.Linq;
using UnityEngine.Assertions;

namespace DepthSensor.Buffer {
    public abstract class AbstractBuffer : IBuffer {
        public readonly int length;
        
        protected AbstractBuffer(int len) {
            this.length = len;
        }
        
        public static bool ReCreateIfNeed<T>(ref T buffer, int len) where T : AbstractBuffer {
            if (buffer == null || buffer.length != len) {
                var type = buffer?.GetType() ?? typeof(T);
                buffer?.Dispose();
                buffer = (T) Create(type, new object[] {len});
                return true;
            }
            return false;
        }
        
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

        public abstract long LengthInBytes();

        protected internal abstract object[] GetArgsForCreateSome();

        protected internal abstract void Set(byte[] newData);

        protected internal abstract void Set(IntPtr newData);

        protected internal abstract void SetBytes(IntPtr newData, long copyBytes);

        public virtual void Dispose() { }
    }
}