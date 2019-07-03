using System;

namespace DepthSensor.Stream {
    public class ArrayStream<T> : AbstractStream {
        public readonly T[] data;

        public ArrayStream(int len) {
            data = new T[len];
            _onActiveChanged = stream => {throw new NotImplementedException();};
        }
        
        public ArrayStream(T[] data) {
            this.data = data;
            _onActiveChanged = stream => {throw new NotImplementedException();};
        }

        public ArrayStream(bool available) : this(0) {
            Available = available;
        }

        public override void Dispose() {}

        public new class Internal : AbstractStream.Internal {
            private readonly ArrayStream<T> _stream;

            protected internal Internal(ArrayStream<T> stream) : base(stream) {
                _stream = stream;
            }

            protected internal void NewFrame(T[] newData) {
                Set(newData);
                OnNewFrame();
            }

            protected internal virtual void Set(T[] newData) {
                Array.Copy(newData, _stream.data, newData.Length);
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