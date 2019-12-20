using UnityEngine;

namespace Games.Common.ColliderGenerator {
    public class DataMouse : BaseColliderGeneratorDataProvider {
        public Vector2 MousePos;
        public float CircleSize;
        
        public override bool IsShapePixel(int x, int y) {
            return Vector2.Distance(new Vector2(x, y), MousePos) <= CircleSize;
        }
    }
}