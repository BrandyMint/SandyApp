using System.Collections.Generic;
using UnityEngine;

namespace Games.Common.ColliderGenerator {
    public class OutputPolygonCollider2DBase : IColliderGeneratorOutput {
        public PolygonCollider2D collider;

        public void Clear() {
            collider.pathCount = 0;
        }

        public virtual void AddShape(List<Vector2> points) {
            var path = collider.pathCount++;
            collider.SetPath(path, points);
        }

        public virtual void PrepareFrame() {}

        public bool IsEmpty() {
            return collider.pathCount == 0;
        }
    }
}