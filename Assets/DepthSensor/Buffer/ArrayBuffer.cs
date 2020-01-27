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
            Array.Copy(newData, data, newData.Length);
        }

        public override long LengthInBytes() {
            return data.GetLengthInBytes();
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
            Array.Clear(data, 0, data.Length);
        }

        protected internal override void Set(byte[] newData) {
            MemUtils.CopyBytes(newData, data);
        }

        protected internal override void Set(IntPtr newData) {
            MemUtils.Copy(newData, data);
        }

        protected internal override void SetBytes(IntPtr newData, long copyBytes) {
            MemUtils.CopyBytes(newData, data, copyBytes);
        }
    }
}