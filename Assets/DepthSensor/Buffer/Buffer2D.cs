using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace DepthSensor.Buffer {
    public abstract class Buffer2D : ArrayBuffer {
        public readonly int width;
        public readonly int height;

        protected Buffer2D(int width, int height) : base(width * height) {
            this.width = width;
            this.height = height;
        }
        
        public static bool ReCreateIfNeed<T>(ref T buffer, int width, int height) where T : Buffer2D {
            var needCreate = buffer == null || buffer.width != width || buffer.height != height;
            if (needCreate) {
                buffer?.Dispose();
                buffer = Create<T>(new object[] {width, height});
                return true;
            }
            return false;
        }
    }
    
    public class Buffer2D<T> : Buffer2D where T : struct {
        public NativeArray<T> data;

        protected bool _wasAlloc;
        
        public Buffer2D(int width, int height, bool alloc) : base(width, height) {
            _wasAlloc = alloc;
            if (alloc)
                data = new NativeArray<T>(width * height, Allocator.Persistent);
        }

        public Buffer2D(int width, int height, T[] data = null) : base(width, height) {
            _wasAlloc = true;
            this.data = data != null 
                    ? new NativeArray<T>(data, Allocator.Persistent)
                    : new NativeArray<T>(width * height, Allocator.Persistent);
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
            lock (SyncRoot) {
                var val = default(T);
                Parallel.For(0, length, i => { data[i] = val; });
            }
        }

        protected internal virtual void Set(T[] newData) {
            lock (SyncRoot) {
                data.CopyFrom(newData);
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

        public override void Dispose() {
            base.Dispose();
            if (_wasAlloc)
                data.Dispose();
        }

        public int GetIFrom(int x, int y) {
            return y * width + x;
        }

        public Vector2 GetXYFrom(int i) {
            return new Vector2(
                i % width,
                i / width
            );
        }
        
        public Vector2Int GetXYiFrom(int i) {
            return new Vector2Int(
                i % width,
                i / width
            );
        }
    }
}