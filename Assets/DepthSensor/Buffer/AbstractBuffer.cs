using System;
using System.Linq;
using UnityEngine.Assertions;

namespace DepthSensor.Buffer {
    public abstract class AbstractBuffer : IBuffer {
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