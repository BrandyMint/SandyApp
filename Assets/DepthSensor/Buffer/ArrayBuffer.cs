using System;

namespace DepthSensor.Buffer {
    public abstract class ArrayBuffer : AbstractBuffer {
        public readonly int length;
        
        protected ArrayBuffer(int len) {
            this.length = len;
        }
        
        public static bool ReCreateIfNeed<T>(ref T buffer, int len) where T : ArrayBuffer {
            bool needCreate = buffer == null || buffer.length != len;
            if (needCreate) {
                buffer?.Dispose();
                buffer = Create<T>(new object[] {len});
                return true;
            }
            return false;
        }
    }
    
    public class ArrayBuffer<T> : ArrayBuffer {
        public readonly T[] data;

        public ArrayBuffer(int len) : base(len) {
            data = new T[len];
        }
        
        public ArrayBuffer(T[] data) : base(data.Length) {
            this.data = data;
        }

        protected internal virtual void Set(T[] newData) {
            lock (SyncRoot) {
                Array.Copy(newData, data, newData.Length);
            }
        }

        protected internal override object[] GetArgsForCreateSome() {
            return new object[] {length};
        }

        public override T1 Copy<T1>() {
            var copy = CreateSome<T1>();
            var buff = copy as ArrayBuffer<T>;
            Array.Copy(data, buff.data, length);
            return copy;
        }

        public override void Clear() {
            lock (SyncRoot) {
                Array.Clear(data, 0, length);
            }
        }

        protected internal override void Set(IntPtr newData) {
            lock (SyncRoot) {
                MemUtils.Copy(newData, data);
            }
        }

        protected internal override void SetBytes(IntPtr newData, long copyBytes) {
            lock (SyncRoot) {
                MemUtils.CopyBytes(newData, data, copyBytes);
            }
        }
    }
}