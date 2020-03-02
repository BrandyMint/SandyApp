using DepthSensorSandbox.Processing;
using UnityEngine;

namespace Games.Common.ColliderGenerator {
    public interface IColliderGeneratorDataProvider {
        Sampler Sampler { get; set; }
        bool IsShapePixel(int x, int y);
        bool IsShapePixel(Vector2Int p);
    }
}