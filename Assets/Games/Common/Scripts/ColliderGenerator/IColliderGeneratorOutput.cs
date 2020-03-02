using System.Collections.Generic;
using UnityEngine;

namespace Games.Common.ColliderGenerator {
    public interface IColliderGeneratorOutput {
        void Clear();
        void AddShape(List<Vector2> points);
        void PrepareFrame();
        bool IsEmpty();
    }
}