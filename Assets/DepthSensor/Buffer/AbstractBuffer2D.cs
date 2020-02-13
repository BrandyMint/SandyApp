using UnityEngine;

namespace DepthSensor.Buffer {
    public abstract class AbstractBuffer2D : AbstractBuffer {
        public readonly int width;
        public readonly int height;

        protected AbstractBuffer2D(int width, int height) {
            this.width = width;
            this.height = height;
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