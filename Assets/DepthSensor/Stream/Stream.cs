using System;
using Unity.Collections;
using UnityEngine;

namespace DepthSensor.Stream {
    public class Stream<T> : AbstractStream where T : struct {
        public readonly int width;
        public readonly int height;
        public NativeArray<T> data;
        
        public Stream(int width, int height, bool alloc) {
            this.width = width;
            this.height = height;
            if (alloc)
                data = new NativeArray<T>(width * height, Allocator.Persistent);
            _onActiveChanged = stream => {throw new NotImplementedException();};
        }

        public Stream(int width, int height, T[] data = null) {
            this.width = width;
            this.height = height;
            this.data = data != null 
                    ? new NativeArray<T>(data, Allocator.Persistent)
                    : new NativeArray<T>(width * height, Allocator.Persistent);
            _onActiveChanged = stream => {throw new NotImplementedException();};
        }

        public Stream(bool available) : this(0, 0) {
            Available = available;
        }

        public override void Dispose() {
            data.Dispose();
        }

        public Vector2 GetXYFrom(long i) {
            return new Vector2(
                i % width,
                i / width
            );
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

        public new class Internal : AbstractStream.Internal {
            private readonly Stream<T> _stream;

            protected internal Internal(Stream<T> stream) : base(stream) {
                _stream = stream;
            }
            
            protected internal void NewFrame(T[] newData) {
                Set(newData);
                OnNewFrame();
            }
            
            protected internal virtual void Set(T[] newData) {
                _stream.data.CopyFrom(newData);
            }
            
            protected internal virtual void Set(IntPtr newData) {
                MemUtils.Copy(newData, _stream.data);
            }
            
            protected internal virtual void SetBytes(IntPtr newData, long copyBytes) {
                MemUtils.CopyBytes(newData, _stream.data, copyBytes);
            }
        }
    }
}