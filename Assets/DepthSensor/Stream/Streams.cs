using UnityEngine;

namespace DepthSensor.Stream {
    public class ColorStream : TextureStream<byte> {
        public ColorStream(int width, int height, TextureFormat format) : base(width, height, format) { }
        public ColorStream(bool available) : base(available) { }
    }

    public class DepthStream : Stream<ushort> {
        public DepthStream(int width, int height, bool alloc) : base(width, height, alloc) { }
        public DepthStream(int width, int height, ushort[] data = null) : base(width, height, data) { }
        public DepthStream(bool available) : base(available) { }
    }

    public class IndexStream : Stream<byte> {
        public IndexStream(int width, int height, bool alloc) : base(width, height, alloc) { }
        public IndexStream(int width, int height, byte[] data = null) : base(width, height, data) { }
        public IndexStream(bool available) : base(available) { }
    }

    public class MapDepthToCameraStream : Stream<Vector2> {
        public MapDepthToCameraStream(int width, int height, bool alloc) : base(width, height, alloc) { }
        public MapDepthToCameraStream(int width, int height, Vector2[] data = null) : base(width, height, data) { }
        public MapDepthToCameraStream(bool available) : base(available) { }
    }
}