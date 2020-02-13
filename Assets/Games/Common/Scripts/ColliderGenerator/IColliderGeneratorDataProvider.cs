using UnityEngine;

namespace Games.Common.ColliderGenerator {
    public interface IColliderGeneratorDataProvider {
        RectInt Rect { get; set; }
        bool IsShapePixel(int x, int y);
        bool IsShapePixel(Vector2Int p);
    }
}