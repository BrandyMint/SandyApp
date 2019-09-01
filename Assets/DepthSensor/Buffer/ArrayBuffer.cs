using System;

namespace DepthSensor.Buffer {
    public class ArrayBuffer<T> : AbstractBuffer {
        public readonly T[] data;

        public ArrayBuffer(int len) {
            data = new T[len];
        }
        
        public ArrayBuffer(T[] data) {
            this.data = data;
        }

        protected internal virtual void Set(T[] newData) {
            lock (SyncRoot) {
                Array.Copy(newData, data, newData.Length);
            }
        }

        protected internal override object[] GetArgsForCreateSome() {
            return new object[] {data.Length};
        }

        public override T1 Copy<T1>() {
            var copy = CreateSome<T1>();
            var buff = copy as ArrayBuffer<T>;
            Array.Copy(data, buff.data, data.Length);
            return copy;
        }

        public override void Clear() {
            lock (SyncRoot) {
                Array.Clear(data, 0, data.Length);
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