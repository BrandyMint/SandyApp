using Unity.Mathematics;
using UnityEngine;

namespace DepthSensor.Buffer {
    public class ColorBuffer : TextureBuffer<byte> {
        public ColorBuffer(int width, int height, TextureFormat format) : base(width, height, format) { }
    }

    public class DepthBuffer : TextureBuffer<ushort> {
        public DepthBuffer(int width, int height) : base(width, height, TextureFormat.R16) { }

        protected internal override object[] GetArgsForCreateSome() {
            return new object[] {width, height};
        }

        public bool IsDepthValid() {
            var middle = data.Length / 2;
            return data[middle] != 0;
        }
    }

    public class IndexBuffer : Buffer2D<byte> {
        public IndexBuffer(int width, int height, bool alloc) : base(width, height, alloc) { }
        public IndexBuffer(int width, int height, byte[] data = null) : base(width, height, data) { }
    }

    public class MapDepthToCameraBuffer : TextureBuffer<half2> {
        public MapDepthToCameraBuffer(int width, int height) : base(width, height, TextureFormat.RGHalf) { }
        
        protected internal override object[] GetArgsForCreateSome() {
            return new object[] {width, height};
        }
    }
}