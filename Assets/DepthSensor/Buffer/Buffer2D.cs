﻿using System;
using System.Threading.Tasks;
using Unity.Collections;

namespace DepthSensor.Buffer {
    public class Buffer2D<T> : AbstractBuffer2D where T : struct {
        public NativeArray<T> data;

        protected bool _wasAlloc;
        
        public Buffer2D(int width, int height) : this(width, height, true) {}
        
        public Buffer2D(int width, int height, bool alloc) : base(width, height) {
            _wasAlloc = alloc;
            if (alloc)
                data = new NativeArray<T>(width * height, Allocator.Persistent);
        }

        public Buffer2D(int width, int height, T[] data) : base(width, height) {
            _wasAlloc = true;
            this.data = new NativeArray<T>(data, Allocator.Persistent);
        }

        public override long LengthInBytes() {
            return data.GetLengthInBytes();
        }

        protected internal override object[] GetArgsForCreateSome() {
            return new object[] {width, height, _wasAlloc};
        }

        public override T1 Copy<T1>() {
            var copy = CreateSome<T1>();
            var buff = copy as Buffer2D<T>;
            data.CopyTo(buff.data);
            return copy;
        }

        public override void Clear() {
            var val = default(T);
            Parallel.For(0, length, i => { data[i] = val; });
        }

        protected internal override void Set(byte[] newData) {
            MemUtils.CopyBytes(newData, data);
        }

        protected internal virtual void Set(T[] newData) {
            data.CopyFrom(newData);
        }
            
        protected internal override void Set(IntPtr newData) {
            MemUtils.Copy(newData, data);
        }
            
        protected internal override void SetBytes(IntPtr newData, long copyBytes) {
            MemUtils.CopyBytes(newData, data, copyBytes);
        }

        public override void Dispose() {
            base.Dispose();
            if (_wasAlloc && data.IsCreated)
                data.Dispose();
        }
    }
}