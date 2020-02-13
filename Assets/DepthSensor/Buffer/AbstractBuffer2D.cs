using UnityEngine;
using UnityEngine.Assertions;

namespace DepthSensor.Buffer {
    public abstract class AbstractBuffer2D : AbstractBuffer {
        public readonly int width;
        public readonly int height;

        protected AbstractBuffer2D(int width, int height) : base(width * height) {
            this.width = width;
            this.height = height;
        }

        public static bool ReCreateIfNeed<T>(ref T buffer, int width, int height) where T : AbstractBuffer2D {
            Assert.IsNotNull(buffer, "Create buffer before using recreate!");
            if (buffer.width != width || buffer.height != height) {
                var type = buffer.GetType();
                buffer?.Dispose();
                buffer = (T) Create(type, new object[] {width, height});
                return true;
            }
            return false;
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