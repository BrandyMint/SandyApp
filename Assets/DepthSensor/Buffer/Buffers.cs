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
    
    public class InfraredBuffer : TextureBuffer<byte> {
        public InfraredBuffer(int width, int height, TextureFormat format) : base(width, height, format) { }
    }

    public class IndexBuffer : TextureBuffer<byte> {
        public IndexBuffer(int width, int height) : base(width, height, TextureFormat.R8) { }
        
        protected internal override object[] GetArgsForCreateSome() {
            return new object[] {width, height};
        }
    }

    public class MapDepthToCameraBuffer : TextureBuffer<half2> {
        public MapDepthToCameraBuffer(int width, int height) : base(width, height, TextureFormat.RGHalf) { }
        
        protected internal override object[] GetArgsForCreateSome() {
            return new object[] {width, height};
        }
    }
}