using UnityEngine;

namespace Games.Common.ColliderGenerator {
    public abstract class BaseColliderGeneratorDataProvider : IColliderGeneratorDataProvider {
        public RectInt Rect { get; set; }

        public bool IsShapePixel(Vector2Int p) {
            return IsShapePixel(p.x, p.y);
        }

        public abstract bool IsShapePixel(int x, int y);

        protected int GetIFrom(int x, int y) {
            return y * Rect.width + x;
        }
    }
}